using AlicatMFCRemastered.Commands.Responses;
using AlicatMFCRemastered.Commands.Responses.Streamed;
using AlicatMFCRemastered.Models;
using Ares.Datamodel;
using Ares.Device;
using Ares.Device.Serial;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using UnitsNet;
using UnitsNet.Units;

namespace AlicatMFCRemastered;

public class MassFlowController : AresDevice, IMassFlowController
{
  private readonly int _expectedDataFormatEntryCount;
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
  private CancellationTokenSource _stateGetterLoopTokenSource = new();
  private CompositeDisposable _stateWatchers = new();
  private Task _stateUpdater = Task.CompletedTask;
  readonly ILogger<MassFlowController> _logger;
  private List<GasInfoEntry> _gases = new();
  private List<ManufacturerInfoEntry> _manufacturerInfo = new();
  private List<DataFrameFormatEntry> _dataFrameFormatEntries = new();
  private LiveDataResponse? _liveData;
  private readonly IAresSerialConnection _serialConnection;

  public MassFlowController(string name, AresStruct config, ILogger<MassFlowController> logger) : base(name)
  {
    HasValve = config.Fields["HasValve"]?.BoolValue ?? false;
    MfcType = config.Fields["MfcType"]?.StringValue ?? "";
    _logger = logger;
    StateStream = _stateSubject.AsObservable();
    AssumedId = config.Fields["serialId"].StringValue[0];
    _serialConnection = connection;
    _stateWatchers = new CompositeDisposable
    {
      _serialConnection.GetTransactionStream<LiveDataResponse>().Select(transaction => transaction.Response).Subscribe(UpdateLiveData)
    };

    _expectedDataFormatEntryCount = mfcType == MfcType.Normal ? 12 : 7;
    _stateSubject.OnNext(AresStateBuilder.Create()
      .Add("Id", AssumedId.ToString())
      .Add("Name", Name)
      .Add("HasValve", HasValve)
      .Add("Firmware", FirmwareVersion)
      .AddList("Gases", Array.Empty<AresValue>(), _ => _)
      .Build());
  }

  public async Task<bool> QueryManufacturerInfo()
  {
    if (MfcType == MfcType.Basis2)
    {
      throw new InvalidOperationException("Basis devices cannot query manufacturer info.");
    }

    // for now we just get entry 4 from the manufacturer info as that contains the model number we need to
    // get the flow limit of the device
    var infoIdx = 4;
    var endMarkerReached = false;

    var command = new ManufactureInfoRequest(AssumedId, FirmwareVersion, infoIdx);
    try
    {
      var response = await GetResponseWithRetry<ManufacturerInfoEntry, ManufactureInfoRequest>(command, 5, TimeSpan.FromSeconds(10));
      _manufacturerInfo.Add(response);
      UpdateManufacturerInfo(response);
      UpdatePotentialMaxValue(response);
      endMarkerReached = response.IsEndMarker;
    }
    catch (TimeoutException e)
    {
      Trace.WriteLine($"Timed out while trying to get manufacturer info: {e.Message}");
      endMarkerReached = true;
    }

    return _manufacturerInfo?.Any(info => info.ManufacturerInfoEntryType == ManufacturerInfoEntryType.ModelNumber) ?? false;
  }

  /// <summary>
  /// Finds a potential max value from a manufacturer info entry containing the MFC model number
  /// assuming it's something similar to "MC-500SCCM-D" and applies it to a data frame format if found
  /// </summary>
  private void UpdatePotentialMaxValue(ManufacturerInfoEntry entry)
  {
    if (entry.EntryNumber != 4)
      return;

    var dataFrameFormat = _dataFrameFormatEntries?.FirstOrDefault(entry => entry.Field == DataFormatField.Setpoint);
    if (dataFrameFormat is not null)
    {
      if (dataFrameFormat.MaxVal is null)
      {
        var modelNumber = entry.Data.Split('-').Skip(1).FirstOrDefault();
        if (modelNumber is null)
        {
          _logger.LogWarning("Failed to get max value for MFC {Name} with model number {Model}", Name, entry.Data);
          return;
        }
        var numMatch = Regex.Match(modelNumber, @"\d+");
        var num = numMatch.Success ? numMatch.Value : default;
        var unitMatch = Regex.Match(modelNumber, @"[A-Z]+");
        if (unitMatch.Success)
        {
          var unitFound = MfcUnitParser.Parser.TryParse<StandardVolumeFlowUnit>(unitMatch.Value, out var unit);
          if (!unitFound)
          {
            _logger.LogWarning("Failed to get max value for MFC {Name} as we couldn't get the value units from model number {Model}", Name, entry.Data);
            return;
          }
          if (!int.TryParse(num, out var numericNum) || numericNum <= 0)
          {
            _logger.LogWarning(
                "Failed to get max value for MFC {Name} as we couldn't get the numeric max value from model number {Model}",
                Name, entry.Data);
            return;
          }
          var flowVal = StandardVolumeFlow.From(numericNum, unit);
          dataFrameFormat.MaxVal = flowVal.StandardLitersPerMinute.ToString();
        }
      }
    }
  }

  public async Task ChangeHardwareUnitId(char targetId)
  {
    await StopUpdateLoop();
    if (Connection is null)
      throw new InvalidOperationException("Connection was null when trying to change hardware id");

    var reservedId = Connection.ReserveId(targetId);

    if (!reservedId)
      throw new InvalidOperationException($"ID {targetId} is already in use by another Alicat");

    if (MfcType == MfcType.Basis2)
    {
      await ChangeBasisHardwareUnitId(targetId);
    }
    else if (MfcType == MfcType.Normal)
    {
      await ChangeNormalHardwareUnitId(targetId);
    }

  }

  private async Task ChangeNormalHardwareUnitId(char targetId)
  {
    var command = new ChangeIdCommand(AssumedId, targetId, FirmwareVersion);
    GenericLineResponse? result = null;
    try
    {
      result = await Connection.Send(command, TimeSpan.FromSeconds(10), CancellationToken.None, response => response.Id == targetId);
    }
    catch (TimeoutException)
    {
    }

    if (result is null)
    {
      Connection.ReleaseId(targetId);
      throw new InvalidOperationException("Could not get a response for the newly changed id");
    }

    Connection.ReleaseId(AssumedId);
    AssumedId = targetId;
    try
    {
      await Initialize();
    }
    catch (Exception e)
    {
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Failed to initialize: {e.Message}" };
    }
  }

  private async Task ChangeBasisHardwareUnitId(char targetId)
  {
    var command = new BasisChangeIdCommand(AssumedId, targetId, GetFormatEntries(), FirmwareVersion);
    await Connection.Send(command);
    await Task.Delay(500); // we don't get a response back immediately, so we have to assume slight delay

    try
    {
      var liveData = await GetLiveData();
    }
    catch (TimeoutException)
    {
      Connection.ReleaseId(targetId);
      throw new InvalidOperationException("Could not get a response for the newly changed id");
    }

    Connection.ReleaseId(AssumedId);
    AssumedId = targetId;
    try
    {
      await Initialize();
    }
    catch (TimeoutException e)
    {
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Failed to initialize: {e.Message}" };
    }
  }

  public Task CancelValveHold()
  {
    var cancelValveHoldRequest = new CancelValveHoldCommand(AssumedId, GetFormatEntries(), FirmwareVersion);
    return Send(cancelValveHoldRequest);
  }

  public Task ChooseDifferentGas(int gasNumber)
  {
    var chooseDifferentGasCommand = new ChooseDifferentGasCommand(AssumedId, gasNumber, GetFormatEntries(), FirmwareVersion, MfcType);
    return Send(chooseDifferentGasCommand);
  }

  public async Task SetSetpointSource(SetpointSource source)
  {
    var request = new SetSetpointSourceCommand(AssumedId, source, ":)");

    await Send(request);
  }

  public async Task<SetpointSource> GetSetpointSource()
  {
    if (MfcType != MfcType.Basis2)
      return SetpointSource.UnknownSource;

    var request = new GetSetpointSourceCommand(AssumedId);
    var response = await Send(request);
    return response.Source;
  }

  public double? GetSetpoint()
    => _liveData?.Setpoint?.Value;


  private async Task QueryBasisGasList()
  {
    var request = new BasisQueryGasCommand(AssumedId, ":)");

    var response = await Send(request, TimeSpan.FromSeconds(10));

    foreach (var gasInfo in response.GasInfoEntries)
    {
      UpdateGasInfo(gasInfo);
    }
  }

  // TODO: Implement a more qualified response to the query, the documentation doesn't show the response syntax
  public async Task<bool> QueryGasListInfo()
  {
    var gasIdx = 0;
    var endMarkerReached = false;
    while (!endMarkerReached)
    {
      var command = new QueryGasCommand(AssumedId, FirmwareVersion, MfcType, gasIdx);
      try
      {
        var response = await GetResponseWithRetry<GasInfoEntry, QueryGasCommand>(command, 5, TimeSpan.FromSeconds(10));
        UpdateGasInfo(response);
        endMarkerReached = response.IsEndMarker;
      }
      catch (TimeoutException e)
      {
        Trace.WriteLine($"Timed out while trying to get gas info entry: {e.Message}");
        endMarkerReached = true;
      }

      gasIdx++;
    }

    return _gases.Count() > 0;
  }

  public async Task QueryFirmwareVersion()
  {
    var request = new MfcFirmwareRequest(AssumedId);
    try
    {
      var response = await Send(request, TimeSpan.FromSeconds(10));
      FirmwareVersion = response.FirmwareVersion;
    }
    catch (OperationCanceledException)
    {
      FirmwareVersion = string.Empty;
    }
    catch (TimeoutException)
    {
      FirmwareVersion = string.Empty;
    }
  }

  public override Task<AresStruct> GetState()
  {
    var newState = AresStateBuilder.Create()
      .Add("Id", AssumedId.ToString())
      .Add("Name", Name)
      .Add("HasValve", HasValve)
      .Add("Firmware", FirmwareVersion)

      .AddStruct("LiveData", b =>
      {
        b.Add("Absolute Pressure", _liveData?.AbsolutePressure?.Value ?? -1.0)
        .Add("Temperature", _liveData?.Temperature?.Value ?? -1.0)
        .Add("Flow", _liveData?.MassFlow?.Value ?? -1.0);
      })

      .AddList("Gases", _gases, gas =>
      {
        var gasStruct = AresStateBuilder.Create()
        .Add("Gas", gas.Gas)
        .Add("Id", gas.Id.ToString())
        .Add("Index", gas.Index)
        .Add("IsEndMarker", gas.IsEndMarker)
        .Build();

        return new AresValue { StructValue = gasStruct };
      })
      .Build();

    return Task.FromResult(newState);
  }

  private void UpdateBasisDataFrames()
  {
    var formatEntries = new DataFrameFormatEntry[] {
      new(AssumedId, 1, DataFormatField.UnitId, "string", null, null, null, "", null),
      new(AssumedId, 2, DataFormatField.Temperature, "s decimal", null, null, null, "", TemperatureUnit.DegreeCelsius),
      new(AssumedId, 3, DataFormatField.Mass, "s decimal", null, null, null, "", StandardVolumeFlowUnit.StandardLiterPerMinute),
      new(AssumedId, 4, DataFormatField.TotalizedMassFlow, "s decimal", null, null, null, "", StandardVolumeFlowUnit.StandardLiterPerMinute),
      new(AssumedId, 5, DataFormatField.Setpoint, "s decimal", null, null, null, "", StandardVolumeFlowUnit.StandardLiterPerMinute),
      new(AssumedId, 6, DataFormatField.ValveDrive, "s decimal", null, null, null, "", null),
      new(AssumedId, 7, DataFormatField.Gas, "string", null, null, null, "", null),
      new(AssumedId, 8, DataFormatField.Status, "string", null, null, "3", "TOV", null),
      new(AssumedId, 8, DataFormatField.Status, "string", null, null, "3", "MOV", null),
      new(AssumedId, 8, DataFormatField.Status, "string", null, null, "3", "OVR", null),
      new(AssumedId, 8, DataFormatField.Status, "string", null, null, "3", "HLD", null),
      new(AssumedId, 8, DataFormatField.Status, "string", null, null, "3", "VTM", null),
    };

    foreach (var entry in formatEntries)
    {
      UpdateDataFrameFormat(entry);
    }
  }

  public async Task<bool> QueryDataFrameFormat()
  {
    var formatIdx = 0;
    var endMarkerReached = false;
    while (!endMarkerReached)
    {
      var command = new DataFormatRequest(AssumedId, FirmwareVersion, formatIdx);
      try
      {
        var response = await GetResponseWithRetry<DataFrameFormatEntry, DataFormatRequest>(command, 5, TimeSpan.FromSeconds(10));
        UpdateDataFrameFormat(response);
        endMarkerReached = response.EntryType == DataFrameFormatEntryType.EndMarker;
      }
      catch (TimeoutException e)
      {
        Trace.WriteLine($"Timed out while trying to get data frame entry: {e.Message}");
        //throw;
        endMarkerReached = true;
      }

      formatIdx++;
    }

    return _dataFrameFormatEntries.Count() >= 7;
  }

  public Task DeleteComposerMix(int mixNumber)
  {
    var deleteMixCommand = new DeleteComposerMixCommand(AssumedId, mixNumber, GetFormatEntries(), FirmwareVersion);
    return Send(deleteMixCommand);
  }

  public Task HoldValvesAtCurrentPosition()
  {
    if (MfcType == MfcType.Normal)
    {
      var holdValvesCommand = new HoldValvesAtCurrentPositionCommand(AssumedId, GetFormatEntries(), FirmwareVersion);
      return Send(holdValvesCommand);
    }
    else if (MfcType == MfcType.Basis2)
    {
      if (_liveData?.ValveDrive is null)
        return Task.CompletedTask;

      var holdValvesCommand = new BasisHoldValvesAtCurrentPositionCommand(AssumedId, FirmwareVersion, GetFormatEntries(), _liveData.ValveDrive.Value);
      return Send(holdValvesCommand);
    }

    return Task.CompletedTask;

  }

  public Task HoldValvesClosed()
  {
    if (MfcType == MfcType.Normal)
    {
      var holdValvesClosedCommand = new HoldValvesClosedCommand(AssumedId, GetFormatEntries(), FirmwareVersion);
      return Send(holdValvesClosedCommand);
    }
    else if (MfcType == MfcType.Basis2)
    {
      var holdValvesClosedCommand = new BasisHoldValvesClosedCommand(AssumedId, GetFormatEntries(), FirmwareVersion);
      return Send(holdValvesClosedCommand);
    }

    return Task.CompletedTask;

  }

  public Task NewComposerMix(MfcGasComposition composerMix)
  {
    var newMixCommand = new NewComposerMixCommand(AssumedId, composerMix, GetFormatEntries(), FirmwareVersion);
    return Send(newMixCommand);
  }

  public async Task NewSetpoint(StandardVolumeFlow setpoint)
  {
    if (MfcType == MfcType.Normal)
    {
      var newSetpointCommand = new NewSetpointCommand(AssumedId, setpoint, GetFormatEntries(), FirmwareVersion);
      try
      {
        var response = await Send(newSetpointCommand, TimeSpan.FromSeconds(10));
      }
      catch (TimeoutException)
      {
        Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Tried setting setpoint to {setpoint.StandardLitersPerMinute}, but timed out while awaiting response." };
        throw;
      }
    }
    else if (MfcType == MfcType.Basis2)
    {
      var newSetpointCommand = new BasisNewSetpointCommand(AssumedId, setpoint, GetFormatEntries(), FirmwareVersion);
      try
      {
        await Send(newSetpointCommand, TimeSpan.FromSeconds(10));
      }
      catch (TimeoutException)
      {
        Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Tried setting setpoint to {setpoint.StandardLitersPerMinute}, but timed out while awaiting response." };
        throw;
      }
    }
  }

  public Task TareAbsolutePressureWithBarometer()
  {
    // ignore taring on BASIS for now, implement later when/if needed
    if (MfcType != MfcType.Normal)
      return Task.CompletedTask;

    var tarePressureCommand = new TareAbsolutePressureWithBarometerCommand(AssumedId, GetFormatEntries(), FirmwareVersion);
    return Send(tarePressureCommand);
  }

  public Task TareFlow()
  {
    // ignore taring on BASIS for now, implement later when/if needed
    if (MfcType != MfcType.Normal)
      return Task.CompletedTask;

    var tareFlowCommand = new TareFlowCommand(AssumedId, GetFormatEntries(), FirmwareVersion);
    return Send(tareFlowCommand);
  }

  public char AssumedId { get; private set; }

  public override IObservable<AresStruct> StateStream { get; }

  public string FirmwareVersion { get; private set; } = string.Empty;
  public bool HasValve { get; }
  public MfcType MfcType { get; }

  public override async Task<bool> Activate(CancellationToken ct)
  {
    var activated = await base.Activate(ct);
    if (activated)
    {
      try
      {
        await Initialize();
      }
      catch (Exception e)
      {
        Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Failed to initialize: {e.Message}" };
        activated = false;
      }
    }

    return activated;
  }

  public override async Task EnterSafeMode(CancellationToken ct)
  {
    //Set the setpoint to zero, effectively shutting off the MFC.. I think
    await NewSetpoint(StandardVolumeFlow.FromStandardLitersPerMinute(0.0));
    await HoldValvesClosed();
  }

  public async ValueTask DisposeAsync()
  {
    _stateWatchers.Dispose();
    await _stateGetterLoopTokenSource.CancelAsync();
    await _stateUpdater;
    _stateGetterLoopTokenSource.Dispose();
    _stateSubject.OnCompleted();
  }

  private Task<LiveDataResponse> GetLiveData()
  {
    var formatEntries = _dataFrameFormatEntries?.ToArray();
    if (formatEntries is null)
    {
      throw new InvalidOperationException(
        $"Cannot get live data as the format entries have not even been initialized. Need to acquire the format entries first.");
    }

    if (formatEntries.Length < _expectedDataFormatEntryCount)
      throw new InvalidOperationException(
        $"Cannot request live data without knowing format entries. Number of currently known formats: {formatEntries.Length}, Expected at least {_expectedDataFormatEntryCount}");

    var command = new LiveDataRequest(formatEntries, FirmwareVersion);
    return Send(command, TimeSpan.FromSeconds(10));
  }

  private async Task Initialize()
  {
    if (Connection is null)
      throw new NullReferenceException("Initialize was called, but Connection was not set");

    await StopUpdateLoop();
    var state = AresStateBuilder
      .Create()
      .Add("HasValve", HasValve)
      .Build();

    _stateSubject.OnNext(state);

    if (MfcType == MfcType.Normal)
    {
      await InitNormal();
    }
    else if (MfcType == MfcType.Basis2)
    {
      await InitBasis();
    }
  }

  private async Task InitNormal()
  {
    var dataFrameQuerySuccess = await QueryDataFrameFormat();
    if (!dataFrameQuerySuccess)
    {
      throw new InvalidOperationException("Failed to query the data frames.");
    }


    var importantEntries = Enumerable.Range(1, 7);
    if (!importantEntries.All(entryNum => _dataFrameFormatEntries?.Any(entry => entry.EntryNumber == entryNum) ?? false))
    {
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = "Did not receive Data Frame Entries 1-7. Could be missing one, could be missing all." };
      return;
    }
    var gasQuerySuccess = await QueryGasListInfo();
    if (!gasQuerySuccess)
    {
      throw new InvalidOperationException("Failed to query the gas list.");
    }
    await QueryFirmwareVersion();
    var manufacturerInfoQuerySuccess = await QueryManufacturerInfo();
    if (!manufacturerInfoQuerySuccess)
    {
      throw new InvalidOperationException("Failed to query the manufacturer info.");
    }
  }

  private async Task InitBasis()
  {
    UpdateBasisDataFrames();
    await QueryBasisGasList();
    await QueryFirmwareVersion();
  }

  public async Task StartUpdateLoop(TimeSpan interval)
  {
    await StopUpdateLoop();
    await Task.Delay(150);
    _stateGetterLoopTokenSource = new CancellationTokenSource();
    _stateUpdater = Task.Factory.StartNew(async _ =>
    {
      Thread.CurrentThread.Name = $"MFC {AssumedId} State Update Loop Thread";
      try
      {
        while (!_stateGetterLoopTokenSource.IsCancellationRequested)
        {
          try
          {
            var liveData = await GetLiveData();
          }
          catch (TimeoutException)
          {
            Status = new DeviceOperationalStatus { OperationalState = OperationalState.Active, Message = $"Get Live Data timed out at {DateTime.Now}" };
          }

          await Task.Delay(interval);
        }
      }
      catch (ObjectDisposedException)
      {
      }
      catch (Exception e)
      {
        Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"{e.Message}" };
      }
    },
      _stateGetterLoopTokenSource.Token);
  }

  private async Task StopUpdateLoop()
  {
    _stateGetterLoopTokenSource.Cancel();
    await _stateUpdater;
  }

  protected override async Task<SerialDeviceValidationResult> Validate()
  {
    var request = new GenericLineRequest(AssumedId);
    try
    {
      var response = await GetResponseWithRetry<GenericLineResponse, GenericLineRequest>(request, 5, TimeSpan.FromSeconds(10));
      if (response.Id == AssumedId)
        return new SerialDeviceValidationResult(true);

      // This should never happen, but in case it does lets throw an exception as it's important to figure out
      // why the response id did not match.
      throw new InvalidOperationException($"Requested a live data line for MFC with id of {AssumedId} but got a response with id of {response.Id}");
    }
    catch (TimeoutException)
    {
      return new SerialDeviceValidationResult(false, $"Could not get a valid response for MFC {Name} with an id of {AssumedId} within allotted time.");
    }
  }

  private async Task<TResult> GetResponseWithRetry<TResult, TRequest>(TRequest request, int retries, TimeSpan timeout)
    where TResult : CommandResponse
    where TRequest : MfcCommandExpectingResponse<TResult>
  {
    while (retries >= 0)
    {
      try
      {
        var response = await Send(request, timeout);
        return response;
      }
      catch (TimeoutException)
      {
      }
      retries--;
    }

    throw new TimeoutException($"Timed out while trying to get {request.GetType().Name}. Id : {request.MfcId}");
  }

  private void UpdateLiveData(LiveDataResponse liveResponse)
  {
    var next = AresStateBuilder
      .From(Current)
      .AddStruct("LiveData", b =>
      {
        b.Add("Pressure", liveResponse.AbsolutePressure?.Value ?? double.MinValue);
        b.Add("Temperature", liveResponse.Temperature?.Value ?? double.MinValue);
        b.Add("Flow", liveResponse.MassFlow?.Value ?? double.MinValue);
        b.Add("ValveDrive", liveResponse.ValveDrive ?? double.MinValue);
      })
      .Build();

    _stateSubject.OnNext(next);
  }

  private void UpdateDataFrameFormat(DataFrameFormatEntry formatEntry)
  {
    // Removing this from state data, doesn't seem relevant
    if (formatEntry.EntryType is not DataFrameFormatEntryType.Entry)
      return;

    if (formatEntry.Id != AssumedId)
    {
      return; // TODO: Throw exception? This is causing issues.
    }

    var staleEntries = _dataFrameFormatEntries?.Where(entry => entry.EntryNumber >= formatEntry.EntryNumber).ToArray() ?? Array.Empty<DataFrameFormatEntry>();
    var existingEntries = new List<DataFrameFormatEntry>(_dataFrameFormatEntries ?? new());
    foreach (var staleEntry in staleEntries)
      existingEntries.Remove(staleEntry);

    existingEntries.Add(formatEntry);
    _dataFrameFormatEntries = existingEntries;
  }

  private void UpdateManufacturerInfo(ManufacturerInfoEntry manufactureEntry)
  {
    if (manufactureEntry.Id != AssumedId || _stateSubject.Value is null)
    {
      return; // TODO: Throw exception? This is causing issues.
    }

    _manufacturerInfo ??= new List<ManufacturerInfoEntry>();

    _manufacturerInfo.RemoveAll(
      entry => entry.EntryNumber >= manufactureEntry.EntryNumber);

    _manufacturerInfo.Add(manufactureEntry);


    var newState = AresStateBuilder
      .From(_stateSubject.Value)
      .AddList(
        key: "ManufacturerInfo",
        items: _manufacturerInfo,
        mapper: entry =>
          new AresValue
          {
            StructValue = AresStateBuilder.Create()
              .Add("EntryNumber", entry.EntryNumber)
              .Add("Manufacturer", entry.ManufacturerInfoEntryType.ToString())
              .Add("Data", entry.Data)
              .Add("IsEndMarker", entry.IsEndMarker)
              .Add("Id", entry.Id)
              .Build()
          })
      .Build();

    _stateSubject.OnNext(newState);
  }

  private void UpdateGasInfo(GasInfoEntry gasEntry)
  {
    if (gasEntry.IsEndMarker)
      return;

    var staleEntries = _gases?.Where(entry => entry.Index >= gasEntry.Index) ?? Array.Empty<GasInfoEntry>();
    var existingEntries = new List<GasInfoEntry>(_gases ?? new());
    foreach (var staleEntry in staleEntries)
      existingEntries.Remove(staleEntry);

    existingEntries.Add(gasEntry);
    _gases = existingEntries;

    if (_stateSubject.Value is null)
      return;


    var newState = AresStateBuilder
      .From(_stateSubject.Value)
      .AddList(
        key: "Gases",
        items: _gases,
        mapper: entry =>
        new AresValue
        {
          StructValue = AresStateBuilder.Create()
          .Add("Gas", entry.Gas)
          .Add("Index", entry.Index)
          .Add("IsEndMarker", entry.IsEndMarker)
          .Add("Id", entry.Id)
          .Add("RequestId", entry.RequestId.ToString())
          .Build()
        }
      ).Build();

    _stateSubject.OnNext(newState);
  }

  private DataFrameFormatEntry[] GetFormatEntries()
  {
    var dataFormatEntries = _dataFrameFormatEntries?.Where(entry => entry is not null).ToArray() ?? Array.Empty<DataFrameFormatEntry>();
    return dataFormatEntries!;
  }

  private Task<IObservable<T>> Send<T>(MfcCommandWithStreamedResponse<T> command) where T : CommandResponse
  {
    return Connection.SendAndStream(command);
  }

  private Task Send(MfcCommand command)
  {
    return Connection.Send(command);
  }

  private Task<T> Send<T>(MfcCommandExpectingResponse<T> command) where T : CommandResponse
  {
    return Connection.Send(command, TimeSpan.FromSeconds(10));
  }

  private Task<T> Send<T>(MfcCommandExpectingResponse<T> command, TimeSpan timeout) where T : CommandResponse
  {
    if (command.MfcId != AssumedId)
    {
      throw new InvalidOperationException($"Attempting to send command improperly. {command.MfcId} != {AssumedId}");
    }
    return Connection.Send(command, timeout);
  }

  public async Task Start()
  {
    await StopUpdateLoop();
    await StartUpdateLoop(TimeSpan.FromMilliseconds(500));
  }


  private AresStruct Current => _stateSubject.Value;
