using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Toolkit.Device.UI;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;
using System.Reactive.Linq;
using TicStepperController.Enums;
using TicStepperController.Responses;
using TicStepperController.UI.State;

namespace TicStepperController.UI;

public partial class TicStepperUnitControlViewModel : DeviceUnitControlViewModel<TicStepper>, IAsyncDisposable
{
  private IDisposable? _stateSubscription;

  public TicStepperUnitControlViewModel(TicStepper device) : base(device)
  {
    TicState = new TicStepperState();
    _stateSubscription = device.StateStream
      .Select(TicStepperStateMapper.FromAresStruct)
      .Subscribe(newState =>
      {
        TicState = newState;
        HasValidData = true;
      });

    NextStep = ReactiveCommand.CreateFromTask(() => device.ExecuteCommand(TicStepperCommand.NextStep.ToString(), [], default));
    PreviousStep = ReactiveCommand.CreateFromTask(() => device.ExecuteCommand(TicStepperCommand.PreviousStep.ToString(), [], default));
    HalfStep = ReactiveCommand.CreateFromTask(() => device.ExecuteCommand(TicStepperCommand.HalfStep.ToString(), [], default));
    ExitSafeStart = ReactiveCommand.CreateFromTask(() => device.ExecuteCommand(TicStepperCommand.ExitSafeStart.ToString(), [], default));
    EnterSafeStart = ReactiveCommand.CreateFromTask(() => device.ExecuteCommand(TicStepperCommand.EnterSafeStart.ToString(), [], default));
    
    ViewType = typeof(TicStepperUnitControl);
  }

  public async Task SetTargetPosition(int position)
  {
    await Device.ExecuteCommand(TicStepperCommand.SetTargetPosition.ToString(), 
      [new DeviceCommandArgument { ArgName = "Position", ArgValue = AresValueHelper.CreateNumber(position) }], default);
  }

  public async ValueTask DisposeAsync()
  {
    _stateSubscription?.Dispose();
  }

  [Reactive]
  public partial TicStepperState TicState { get; set; }

  [Reactive]
  public partial bool HasValidData { get; private set; }

  public ReactiveCommand<Unit, CommandResult> NextStep { get; }
  public ReactiveCommand<Unit, CommandResult> PreviousStep { get; }
  public ReactiveCommand<Unit, CommandResult> HalfStep { get; }
  public ReactiveCommand<Unit, CommandResult> ExitSafeStart { get; }
  public ReactiveCommand<Unit, CommandResult> EnterSafeStart { get; }

  public new string DeviceName => Device.Name;
  public int CurrentPosition => TicState.CurrentPosition;

  public TicErrors ErrorStatus => TicState.Errors;
  public TicMiscFlags MiscFlags => TicState.MiscFlags;
  public ErrorsOccurred? ErrorsOccurred => null; 
}
