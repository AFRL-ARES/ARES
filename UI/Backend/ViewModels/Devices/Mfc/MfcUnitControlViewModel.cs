using Ares.Alicat.Mfc.Messaging;
using DynamicData;
using ReactiveUI.Fody.Helpers;
using UI.Pages.Shared.Devices.Mfc;
using UnitsNet;
using UnitsNet.Units;

namespace UI.Backend.ViewModels.Devices.Mfc;

public class MfcUnitControlViewModel : DeviceUnitControlViewModel, IAsyncDisposable
{
  private readonly DeviceRequest _deviceRequest;
  private readonly MfcRpc.MfcRpcClient _mfcClient;
  private readonly CancellationTokenSource _stateStreamCts = new();
  private Task _stateListener = Task.CompletedTask;

  public MfcUnitControlViewModel(string mfcId, string mfcName, MfcRpc.MfcRpcClient mfcClient) : base(mfcId, mfcName)
  {
    MfcName = mfcName;
    _deviceRequest = new DeviceRequest { DeviceId = DeviceId };
    _mfcClient = mfcClient;
    ViewType = typeof(MfcUnitControl);
    Initialize();

    DefaultWidth = 19;
  }

  public string MfcName { get; }

  [Reactive]
  public int TargetGas { get; set; }

  [Reactive]
  public double? TargetSetpoint { get; set; }

  [Reactive]
  public bool CapturingLiveData { get; private set; }

  [Reactive]
  public IEnumerable<GasInfoEntry>? AvailableGases { get; set; }

  [Reactive]
  public string? SelectedGas { get; set; }

  [Reactive]
  public char? AssumedId { get; set; }

  [Reactive]
  public bool HasValve { get; private set; }

  [Reactive]
  public Temperature? Temperature { get; set; }

  [Reactive]
  public Pressure? AbsolutePressure { get; set; }

  [Reactive]
  public VolumeFlow? VolumetricFlow { get; set; }

  [Reactive]
  public StandardVolumeFlow? MassFlow { get; set; }

  [Reactive]
  public StandardVolumeFlow? Setpoint { get; set; }

  [Reactive]
  public double? ValveDrive { get; set; }

  [Reactive]
  public bool HasValidData { get; private set; }

  public ISourceList<Status> StatusCodes { get; } = new SourceList<Status>();

  public async ValueTask DisposeAsync()
  {
    _stateStreamCts.Cancel();
    await _stateListener;
    _stateStreamCts.Dispose();

    GC.SuppressFinalize(this);
  }

  private void Initialize()
  {
    ListenForStates();
  }

  public void ListenForStates()
  {
    try
    {
      _stateListener = Task.Run(async () =>
      {
        Thread.CurrentThread.Name = $"Mass Flow Controller {DeviceName} State Listener Thread";
        var state = await _mfcClient.GetStateAsync(_deviceRequest);
        UpdateState(state);
        CapturingLiveData = true;
        try
        {
          while(!_stateStreamCts.Token.IsCancellationRequested)
          {
            var stateResponse = await _mfcClient.GetStateUpdateAsync(_deviceRequest, null, null, _stateStreamCts.Token);
            UpdateState(stateResponse);
            await Task.Delay(100);
          }
        }
        catch(Exception)
        {
          Console.WriteLine($"~~~~~~~ Exception Getting State, Thread will die probably? ~~~~~~~");
        }

        CapturingLiveData = false;
      },
        _stateStreamCts.Token);
    }
    catch(OperationCanceledException)
    {
    }
  }

  private void UpdateState(StateResponse state)
  {
    AssumedId = state.AssumedId?.FirstOrDefault();
    AvailableGases = state.AvailableGasInfos;
    if(state.Data is null)
    {
      HasValidData = false;
      return;
    }
    HasValidData = true;

    if(state.Data.Temperature is not null)
    {
      var foundTempUnit = UnitsNet.Temperature.TryParseUnit(state.Data.Temperature.Unit, out var tempUnit);
      Temperature = UnitsNet.Temperature.From(state.Data.Temperature.Value, foundTempUnit ? tempUnit : TemperatureUnit.DegreeCelsius);
    }

    if(state.Data.AbsolutePressure is not null)
    {
      var foundAbsolutePressureUnit = Pressure.TryParseUnit(state.Data.AbsolutePressure.Unit, out var pressureUnit);
      AbsolutePressure = Pressure.From(state.Data.AbsolutePressure.Value, foundAbsolutePressureUnit ? pressureUnit : PressureUnit.PoundForcePerSquareInch);
    }

    if(state.Data.VolumetricFlow is not null)
    {
      var foundVolumetricFlowUnit = VolumeFlow.TryParseUnit(state.Data.VolumetricFlow.Unit, out var volumeFlowUnit);
      VolumetricFlow = VolumeFlow.From(state.Data.VolumetricFlow.Value, foundVolumetricFlowUnit ? volumeFlowUnit : VolumeFlowUnit.CubicCentimeterPerMinute);
    }

    if(state.Data.MassFlow is not null)
    {
      var foundMassFlowUnit = StandardVolumeFlow.TryParseUnit(state.Data.MassFlow.Unit, out var massFlowUnit);
      MassFlow = StandardVolumeFlow.From(state.Data.MassFlow.Value, foundMassFlowUnit ? massFlowUnit : StandardVolumeFlowUnit.StandardLiterPerMinute);
    }

    if(state.Data.Setpoint is not null)
    {
      var foundSetPointUnit = StandardVolumeFlow.TryParseUnit(state.Data.Setpoint.Unit, out var setpointUnit);
      Setpoint = StandardVolumeFlow.From(state.Data.Setpoint.Value, foundSetPointUnit ? setpointUnit : StandardVolumeFlowUnit.StandardLiterPerMinute);
    }

    if(state.Data.HasValveDrive)
    {
      ValveDrive = state.Data.ValveDrive;
    }

    HasValve = state.HasValve;

    ParseStatusCodes(state.Data.StatusCodes);
    SelectedGas = state.Data.Gas;
  }

  private void ParseStatusCodes(IEnumerable<Status> statusCodes)
  {
    var statusCodesArr = statusCodes.ToArray();
    var removedCodes = StatusCodes.Items.Except(statusCodesArr);
    var addedCodes = statusCodesArr.Except(StatusCodes.Items);
    StatusCodes.RemoveMany(removedCodes);
    StatusCodes.AddRange(addedCodes);
  }

  public void SetSetpoint()
  {
    if(!TargetSetpoint.HasValue)
      return;
    var setSetpointReq = new SetSetpointRequest { DeviceRequest = _deviceRequest, Setpoint = TargetSetpoint.Value };
    _mfcClient.SetSetpoint(setSetpointReq);
  }

  public Task HoldValvesAtCurrentPosition()
    => _mfcClient.HoldValvesAtCurrentPositionAsync(_deviceRequest).ResponseAsync;

  public Task CancelValveHold()
    => _mfcClient.CancelValveHoldAsync(_deviceRequest).ResponseAsync;

  public Task HoldValvesClose()
    => _mfcClient.HoldValvesClosedAsync(_deviceRequest).ResponseAsync;

  public void TareFLow()
  {
    _mfcClient.TareFlow(_deviceRequest);
  }

  public void TareAbsolutePressureWithBarometer()
  {
    _mfcClient.TareAbsolutePressureWithBarometer(_deviceRequest);
  }
}
