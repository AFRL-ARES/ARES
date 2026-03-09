using AlicatMFCRemastered.Commands.Responses.Streamed;
using AlicatMFCRemastered.UI.State;
using Ares.Toolkit.Device.UI;
using DynamicData;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;
using System.Reactive.Linq;
using UnitsNet;

namespace AlicatMFCRemastered.UI;

public partial class MfcUnitControlViewModel : DeviceUnitControlViewModel<MassFlowController>, IAsyncDisposable
{
  private IDisposable? _stateSubscription;
  private readonly ILogger<MfcUnitControlViewModel> _logger;

  public MfcUnitControlViewModel(MassFlowController mfc, ILogger<MfcUnitControlViewModel> logger) : base(mfc)
  {
    _logger = logger;
    AlicatState = new AlicatMfcState();
    _stateSubscription = mfc.StateStream
      .Select(AlicatStateMapper.FromAresStruct)
      .Subscribe(newState =>
      {
        AlicatState = newState;
        HasValidData = true;
      });

    HoldValvesAtCurrentPositionCommand = ReactiveCommand.CreateFromTask(Device.HoldValvesAtCurrentPosition);
    CancelValveHoldCommand = ReactiveCommand.CreateFromTask(Device.CancelValveHold);
    HoldValvesClosedCommand = ReactiveCommand.CreateFromTask(Device.HoldValvesClosed);
    TareFlowCommand = ReactiveCommand.CreateFromTask(Device.TareFlow);
    TareAbsolutePressureWithBarometerCommand = ReactiveCommand.CreateFromTask(Device.TareAbsolutePressureWithBarometer);
    ViewType = typeof(MfcUnitControl);
    DefaultWidth = 19;

  }

  public async ValueTask DisposeAsync()
  {
    _stateSubscription?.Dispose();
    GC.SuppressFinalize(this);
  }

  public void ListenForStates()
  {
    _stateSubscription?.Dispose();

    _stateSubscription = Device.StateStream
        .Subscribe(
            _ => CapturingLiveData = true,
            ex =>
            {
              _logger.LogError(ex, "State stream failed for {Name}", DeviceName);
              CapturingLiveData = false;
            },
            () => CapturingLiveData = false
        );
  }

  public async Task SetSetpoint()
  {
    if(!TargetSetpoint.HasValue)
      return;

    await Device.NewSetpoint(StandardVolumeFlow.FromStandardLitersPerMinute(TargetSetpoint.Value));
  }

  [Reactive]
  public partial int TargetGas { get; set; }
  [Reactive]
  public partial double? TargetSetpoint { get; set; }
  [Reactive]
  public partial bool CapturingLiveData { get; private set; }
  [Reactive]
  public partial IEnumerable<GasInfoEntry>? AvailableGases { get; set; }
  [Reactive]
  public partial bool HasValidData { get; private set; }
  public ISourceList<Status> StatusCodes { get; } = new SourceList<Status>();
  public ReactiveCommand<Unit, Unit> HoldValvesAtCurrentPositionCommand { get; }
  public ReactiveCommand<Unit, Unit> CancelValveHoldCommand { get; }
  public ReactiveCommand<Unit, Unit> HoldValvesClosedCommand { get; }
  public ReactiveCommand<Unit, Unit> TareFlowCommand { get; }
  public ReactiveCommand<Unit, Unit> TareAbsolutePressureWithBarometerCommand { get; }

  [Reactive]
  public partial AlicatMfcState AlicatState { get; set; }
}

