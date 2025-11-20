using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using AlicatMFC.Commands.Requests;
using AlicatMFC.Commands.Responses;
using AlicatMFC.Commands.Responses.Streamed;
using Ares.Alicat.Mfc.Config;
using Ares.Alicat.Mfc.Messaging;
using Ares.Datamodel.Device;
using Ares.Device.Serial;
using Microsoft.Extensions.Logging;
using UnitsNet;
using UnitsNet.Units;
using DataFrameFormatEntry = AlicatMFC.Commands.Responses.Streamed.DataFrameFormatEntry;
using GasInfoEntry = AlicatMFC.Commands.Responses.Streamed.GasInfoEntry;
using ManufacturerInfoEntry = AlicatMFC.Commands.Responses.ManufacturerInfoEntry;

namespace AlicatMFC;

public class MassFlowController : SerialDevice<IMfcConnection>, IMassFlowController
{
  private readonly int _expectedDataFormatEntryCount;
  private readonly BehaviorSubject<MfcState?> _statePublisher = new(default);
  private CancellationTokenSource _stateGetterLoopTokenSource = new();
  private CompositeDisposable _stateWatchers = new();
  private Task _stateUpdater = Task.CompletedTask;
  readonly ILogger<MassFlowController> _logger;

  public MassFlowController(string name, char id, IMfcConnection connection, bool hasValve, MfcType mfcType, ILogger<MassFlowController> logger) : base(name, connection)
  {
    HasValve = hasValve;
    MfcType = mfcType;
    _logger = logger;
    StateStream = _statePublisher.AsObservable();
    AssumedId = id;
    _stateWatchers = new CompositeDisposable
    {
      Connection.GetTransactionStream<LiveDataResponse>().Select(transaction => transaction.Response).Subscribe(UpdateState)
    };

    _expectedDataFormatEntryCount = mfcType == MfcType.Normal ? 12 : 7;
  }

  public async Task<bool> QueryManufacturerInfo()
  {
    if(MfcType == MfcType.Basis2)
    {
      throw new InvalidOperationException("Basis devices cannot query manufacturer info.");
    }

    var currentState = GetCurrentState();
    if(currentState is null)
      return false;

    // for now we just get entry 4 from the manufacturer info as that contains the model number we need to
    // get the flow limit of the device
    var infoIdx = 4;
    var endMarkerReached = false;

    var command = new ManufactureInfoRequest(currentState.Id, FirmwareVersion, infoIdx);
    try
    {
      var response = await GetResponseWithRetry<ManufacturerInfoEntry, ManufactureInfoRequest>(command, 5, TimeSpan.FromMilliseconds(500));
      UpdateState(response);
      UpdatePotentialMaxValue(response);
      endMarkerReached = response.IsEndMarker;
    }
    catch(TimeoutException e)
    {
      Trace.WriteLine($"Timed out while trying to get manufacturer info: {e.Message}");
      endMarkerReached = true;
    }
    currentState = GetCurrentState();
    return currentState?.ManufacturerInfo?.Any(info => info.ManufacturerInfoEntryType == Ares.Alicat.Mfc.Messaging.ManufacturerInfoEntryType.ModelNumber) ?? false;
  }

  /// <summary>
  /// Finds a potential max value from a manufacturer info entry containing the MFC model number
  /// assuming it's something similar to "MC-500SCCM-D" and applies it to a data frame format if found
  /// </summary>
  private void UpdatePotentialMaxValue(ManufacturerInfoEntry entry)
  {
    if(entry.EntryNumber != 4)
      return;

    var currentState = GetCurrentState();
    var dataFrameFormat = currentState?.DataFrameFormatEntries?.FirstOrDefault(entry => entry.Field == DataFormatField.Setpoint);
    if(dataFrameFormat is not null)
    {
      if(dataFrameFormat.MaxVal is null)
      {
        var modelNumber = entry.Data.Split('-').Skip(1).FirstOrDefault();
        if(modelNumber is null)
        {
          _logger.LogWarning("Failed to get max value for MFC {Name} with model number {Model}", Name, entry.Data);
          return;
        }
        var numMatch = Regex.Match(modelNumber, @"\d+");
        var num = numMatch.Success ? numMatch.Value : default;
        var unitMatch = Regex.Match(modelNumber, @"[A-Z]+");
        if(unitMatch.Success)
        {
          var unitFound = MfcUnitParser.Parser.TryParse<StandardVolumeFlowUnit>(unitMatch.Value, out var unit);
          if(!unitFound)
          {
            _logger.LogWarning("Failed to get max value for MFC {Name} as we couldn't get the value units from model number {Model}", Name, entry.Data);
            return;
          }
          if(!int.TryParse(num, out var numericNum) || numericNum <= 0)
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
    if(Connection is null)
      throw new InvalidOperationException("Connection was null when trying to change hardware id");

    var reservedId = Connection.ReserveId(targetId);

    if(!reservedId)
      throw new InvalidOperationException($"ID {targetId} is already in use by another Alicat");

    if(MfcType == MfcType.Basis2)
    {
      await ChangeBasisHardwareUnitId(targetId);
    }
    else if(MfcType == MfcType.Normal)
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
      result = await Connection.Send(command, TimeSpan.FromSeconds(5), response => response.Id == targetId);
    }
    catch(TimeoutException)
    {
    }

    if(result is null)
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
    catch(TimeoutException e)
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
    catch(TimeoutException)
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
    catch(TimeoutException e)
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
    var currentState = GetCurrentState();
    if(currentState is null)
      return;

    var request = new SetSetpointSourceCommand(currentState.Id, source, ":)");

    await Send(request);
  }

  public async Task<SetpointSource> GetSetpointSource()
  {
    var currentState = GetCurrentState();
    if(currentState is null)
      return SetpointSource.UnknownSource;

    var request = new GetSetpointSourceCommand(currentState.Id);
    var response = await Send(request);
    return response.Source;
  }

  private async Task QueryBasisGasList()
  {
    var currentState = GetCurrentState();
    if(currentState is null)
      return;

    var request = new BasisQueryGasCommand(currentState.Id, ":)");

    var response = await Send(request, TimeSpan.FromSeconds(5));

    foreach(var gasInfo in response.GasInfoEntries)
    {
      UpdateState(gasInfo);
    }
  }

  // TODO: Implement a more qualified response to the query, the documentation doesn't show the response syntax
  public async Task<bool> QueryGasListInfo()
  {
    var currentState = GetCurrentState();
    if(currentState is null)
      return false;

    var gasIdx = 0;
    var endMarkerReached = false;
    while(!endMarkerReached)
    {
      var command = new QueryGasCommand(currentState.Id, FirmwareVersion, MfcType, gasIdx);
      try
      {
        var response = await GetResponseWithRetry<GasInfoEntry, QueryGasCommand>(command, 5, TimeSpan.FromMilliseconds(500));
        UpdateState(response);
        endMarkerReached = response.IsEndMarker;
      }
      catch(TimeoutException e)
      {
        Trace.WriteLine($"Timed out while trying to get gas info entry: {e.Message}");
        endMarkerReached = true;
      }

      gasIdx++;
    }

    currentState = GetCurrentState();
    return currentState?.Gases?.Count() > 0;
  }

  public async Task QueryFirmwareVersion()
  {
    var request = new MfcFirmwareRequest(AssumedId);
    try
    {
      var response = await Send(request, TimeSpan.FromSeconds(3));
      FirmwareVersion = response.FirmwareVersion;
    }
    catch(OperationCanceledException)
    {
      FirmwareVersion = string.Empty;
    }
    catch(TimeoutException)
    {
      FirmwareVersion = string.Empty;
    }
  }

  private void UpdateBasisDataFrames()
  {
    var currentState = GetCurrentState();
    if(currentState is null)
      return;

    var formatEntries = new DataFrameFormatEntry[] {
      new(currentState.Id, 1, DataFormatField.UnitId, "string", null, null, null, "", null),
      new(currentState.Id, 2, DataFormatField.Temperature, "s decimal", null, null, null, "", TemperatureUnit.DegreeCelsius),
      new(currentState.Id, 3, DataFormatField.Mass, "s decimal", null, null, null, "", StandardVolumeFlowUnit.StandardLiterPerMinute),
      new(currentState.Id, 4, DataFormatField.TotalizedMassFlow, "s decimal", null, null, null, "", StandardVolumeFlowUnit.StandardLiterPerMinute),
      new(currentState.Id, 5, DataFormatField.Setpoint, "s decimal", null, null, null, "", StandardVolumeFlowUnit.StandardLiterPerMinute),
      new(currentState.Id, 6, DataFormatField.ValveDrive, "s decimal", null, null, null, "", null),
      new(currentState.Id, 7, DataFormatField.Gas, "string", null, null, null, "", null),
      new(currentState.Id, 8, DataFormatField.Status, "string", null, null, "3", "TOV", null),
      new(currentState.Id, 8, DataFormatField.Status, "string", null, null, "3", "MOV", null),
      new(currentState.Id, 8, DataFormatField.Status, "string", null, null, "3", "OVR", null),
      new(currentState.Id, 8, DataFormatField.Status, "string", null, null, "3", "HLD", null),
      new(currentState.Id, 8, DataFormatField.Status, "string", null, null, "3", "VTM", null),
    };

    foreach(var entry in formatEntries)
    {
      UpdateState(entry);
    }
  }

  public async Task<bool> QueryDataFrameFormat()
  {
    var currentState = GetCurrentState();
    if(currentState is null)
      return false;

    var formatIdx = 0;
    var endMarkerReached = false;
    while(!endMarkerReached)
    {
      var command = new DataFormatRequest(currentState.Id, FirmwareVersion, formatIdx);
      try
      {
        var response = await GetResponseWithRetry<DataFrameFormatEntry, DataFormatRequest>(command, 5, TimeSpan.FromMilliseconds(500));
        UpdateState(response);
        endMarkerReached = response.EntryType == DataFrameFormatEntryType.EndMarker;
      }
      catch(TimeoutException e)
      {
        Trace.WriteLine($"Timed out while trying to get data frame entry: {e.Message}");
        //throw;
        endMarkerReached = true;
      }

      formatIdx++;
    }

    currentState = GetCurrentState();
    return currentState?.DataFrameFormatEntries?.Count() >= 7;
  }

  public Task DeleteComposerMix(int mixNumber)
  {
    var deleteMixCommand = new DeleteComposerMixCommand(AssumedId, mixNumber, GetFormatEntries(), FirmwareVersion);
    return Send(deleteMixCommand);
  }

  public Task HoldValvesAtCurrentPosition()
  {
    if(MfcType == MfcType.Normal)
    {
      var holdValvesCommand = new HoldValvesAtCurrentPositionCommand(AssumedId, GetFormatEntries(), FirmwareVersion);
      return Send(holdValvesCommand);
    }
    else if(MfcType == MfcType.Basis2)
    {
      var currentState = GetCurrentState();
      if(currentState?.LiveData?.ValveDrive is null)
        return Task.CompletedTask;

      var holdValvesCommand = new BasisHoldValvesAtCurrentPositionCommand(AssumedId, FirmwareVersion, GetFormatEntries(), currentState.LiveData.ValveDrive.Value);
      return Send(holdValvesCommand);
    }

    return Task.CompletedTask;

  }

  public Task HoldValvesClosed()
  {
    if(MfcType == MfcType.Normal)
    {
      var holdValvesClosedCommand = new HoldValvesClosedCommand(AssumedId, GetFormatEntries(), FirmwareVersion);
      return Send(holdValvesClosedCommand);
    }
    else if(MfcType == MfcType.Basis2)
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
    if(MfcType == MfcType.Normal)
    {
      var newSetpointCommand = new NewSetpointCommand(AssumedId, setpoint, GetFormatEntries(), FirmwareVersion);
      try
      {
        var response = await Send(newSetpointCommand, TimeSpan.FromMilliseconds(1000));
      }
      catch(TimeoutException)
      {
        Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Tried setting setpoint to {setpoint.StandardLitersPerMinute}, but timed out while awaiting response." };
      }
    }
    else if(MfcType == MfcType.Basis2)
    {
      var newSetpointCommand = new BasisNewSetpointCommand(AssumedId, setpoint, GetFormatEntries(), FirmwareVersion);
      await Send(newSetpointCommand);
    }
  }

  public Task TareAbsolutePressureWithBarometer()
  {
    // ignore taring on BASIS for now, implement later when/if needed
    if(MfcType != MfcType.Normal)
      return Task.CompletedTask;

    var tarePressureCommand = new TareAbsolutePressureWithBarometerCommand(AssumedId, GetFormatEntries(), FirmwareVersion);
    return Send(tarePressureCommand);
  }

  public Task TareFlow()
  {
    // ignore taring on BASIS for now, implement later when/if needed
    if(MfcType != MfcType.Normal)
      return Task.CompletedTask;

    var tareFlowCommand = new TareFlowCommand(AssumedId, GetFormatEntries(), FirmwareVersion);
    return Send(tareFlowCommand);
  }

  public char AssumedId { get; private set; }

  public IObservable<MfcState?> StateStream { get; }

  public string FirmwareVersion { get; private set; } = string.Empty;
  public bool HasValve { get; }
  public MfcType MfcType { get; }

  public override async Task<bool> Activate(CancellationToken ct)
  {
    var activated = await base.Activate(ct);
    if(activated)
    {
      try
      {
        await Initialize();
      }
      catch(TimeoutException e)
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
    _stateGetterLoopTokenSource.Cancel();
    await _stateUpdater;
    _stateGetterLoopTokenSource.Dispose();
    _statePublisher.OnCompleted();
  }

  public Task<LiveDataResponse> GetLiveData()
  {
    var currentState = GetCurrentState();
    if(currentState is null)
    {
      throw new InvalidOperationException("Current state has not yet been initialized. Need to initialize the device first.");
    }
    if(currentState.DataFrameFormatEntries?.Count() < _expectedDataFormatEntryCount)
      throw new InvalidOperationException(
        $"Cannot request live data without knowing format entries. Number of currently known formats: {currentState?.DataFrameFormatEntries?.Count() ?? 0}, Expected at least {_expectedDataFormatEntryCount}");

    var formatEntries =
      currentState.DataFrameFormatEntries?.ToArray() ?? Array.Empty<DataFrameFormatEntry>();

    var command = new LiveDataRequest(formatEntries, FirmwareVersion);
    return Send(command, TimeSpan.FromSeconds(5));
  }

  private async Task Initialize()
  {
    if(Connection is null)
      throw new NullReferenceException("Initialize was called, but Connection was not set");

    await StopUpdateLoop();
    var state = new MfcState(AssumedId, Name)
    {
      HasValve = HasValve
    };

    _statePublisher.OnNext(state);

    if(MfcType == MfcType.Normal)
    {
      await InitNormal();
    }
    else if(MfcType == MfcType.Basis2)
    {
      await InitBasis();
    }
  }

  private async Task InitNormal()
  {
    await QueryDataFrameFormat();
    var cs = GetCurrentState();

    var importantEntries = Enumerable.Range(1, 7);
    if(!importantEntries.All(entryNum => cs!.DataFrameFormatEntries?.Any(entry => entry.EntryNumber == entryNum) ?? false))
    {
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = "Did not receive Data Frame Entries 1-7. Could be missing one, could be missing all." };
      return;
    }
    await QueryGasListInfo();
    await QueryFirmwareVersion();
    await QueryManufacturerInfo();
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
        while(!_stateGetterLoopTokenSource.IsCancellationRequested)
        {
          try
          {
            var liveData = await GetLiveData();
          }
          catch(TimeoutException)
          {
            Status = new DeviceOperationalStatus { OperationalState = OperationalState.Active, Message = $"Get Live Data timed out at {DateTime.Now}" };
          }

          await Task.Delay(interval);
        }
      }
      catch(ObjectDisposedException)
      {
      }
      catch(Exception e)
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
      var response = await GetResponseWithRetry<GenericLineResponse, GenericLineRequest>(request, 5, TimeSpan.FromSeconds(1));
      if(response.Id == AssumedId)
        return new SerialDeviceValidationResult(true);

      // This should never happen, but in case it does lets throw an exception as it's important to figure out
      // why the response id did not match.
      throw new InvalidOperationException($"Requested a live data line for MFC with id of {AssumedId} but got a response with id of {response.Id}");
    }
    catch(TimeoutException)
    {
      return new SerialDeviceValidationResult(false, $"Could not get a valid response for MFC {Name} with an id of {AssumedId} within allotted time.");
    }
  }

  private async Task<TResult> GetResponseWithRetry<TResult, TRequest>(TRequest request, int retries, TimeSpan timeout)
    where TResult : CommandResponse
    where TRequest : MfcCommandExpectingResponse<TResult>
  {
    while(retries >= 0)
    {
      try
      {
        var response = await Send(request, timeout);
        return response;
      }
      catch(TimeoutException)
      {
      }
      retries--;
    }

    throw new TimeoutException($"Timed out while trying to get {request.GetType().Name}. Id : {request.MfcId}");
  }

  private void UpdateState(LiveDataResponse liveResponse)
  {
    var currentState = GetCurrentState();
    if(currentState is null || liveResponse.Id != AssumedId)
    {
      return;
    }
    var newState = currentState with { LiveData = liveResponse };
    _statePublisher.OnNext(newState);
  }

  private void UpdateState(DataFrameFormatEntry formatEntry)
  {
    if(formatEntry.EntryType is not DataFrameFormatEntryType.Entry)
      return;

    var currentState = GetCurrentState();
    if(formatEntry.Id != AssumedId || currentState is null)
    {
      return; // TODO: Throw exception? This is causing issues.
    }
    var staleEntries = currentState.DataFrameFormatEntries?.Where(entry => entry.EntryNumber >= formatEntry.EntryNumber).ToArray() ?? Array.Empty<DataFrameFormatEntry>();
    var existingEntries = new List<DataFrameFormatEntry>(currentState.DataFrameFormatEntries ?? Array.Empty<DataFrameFormatEntry>());
    foreach(var staleEntry in staleEntries)
      existingEntries.Remove(staleEntry);

    existingEntries.Add(formatEntry);
    var newState = currentState with { DataFrameFormatEntries = existingEntries };
    _statePublisher.OnNext(newState);
  }

  private void UpdateState(ManufacturerInfoEntry manufactureEntry)
  {
    var currentState = GetCurrentState();
    if(manufactureEntry.Id != AssumedId || currentState is null)
    {
      return; // TODO: Throw exception? This is causing issues.
    }
    var staleEntries = currentState.ManufacturerInfo?.Where(entry => entry.EntryNumber >= manufactureEntry.EntryNumber).ToArray() ?? Array.Empty<ManufacturerInfoEntry>();
    var existingEntries = new List<ManufacturerInfoEntry>(currentState.ManufacturerInfo ?? Array.Empty<ManufacturerInfoEntry>());
    foreach(var staleEntry in staleEntries)
      existingEntries.Remove(staleEntry);

    existingEntries.Add(manufactureEntry);
    var newState = currentState with { ManufacturerInfo = existingEntries };
    _statePublisher.OnNext(newState);
  }

  private void UpdateState(GasInfoEntry gasEntry)
  {
    if(gasEntry.IsEndMarker)
      return;

    var currentState = GetCurrentState();
    if(currentState is null)
      return;

    var staleEntries = currentState.Gases?.Where(entry => entry.Index >= gasEntry.Index) ?? Array.Empty<GasInfoEntry>();
    var existingEntries = new List<GasInfoEntry>(currentState.Gases ?? Array.Empty<GasInfoEntry>());
    foreach(var staleEntry in staleEntries)
      existingEntries.Remove(staleEntry);

    existingEntries.Add(gasEntry);
    var newState = currentState with { Gases = existingEntries };
    _statePublisher.OnNext(newState);
  }

  private MfcState? GetCurrentState()
  {
    var state = _statePublisher.Value;
    return state;
  }

  private DataFrameFormatEntry[] GetFormatEntries()
  {
    var currentState = GetCurrentState();
    var dataFormatEntries = currentState?.DataFrameFormatEntries?.Where(entry => entry is not null).ToArray() ?? Array.Empty<DataFrameFormatEntry>();

    return dataFormatEntries!;
  }

  private Task<IObservable<T>> Send<T>(MfcCommandWithStreamedResponse<T> command) where T : CommandResponse
  {
    return Connection.Send(command);
  }

  private Task Send(MfcCommand command)
  {
    return Connection.Send(command);
  }

  private Task<T> Send<T>(MfcCommandExpectingResponse<T> command) where T : CommandResponse
  {
    return Connection.Send(command, TimeSpan.FromSeconds(5));
  }

  private Task<T> Send<T>(MfcCommandExpectingResponse<T> command, TimeSpan timeout) where T : CommandResponse
  {
    if(command.MfcId != AssumedId)
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
}
