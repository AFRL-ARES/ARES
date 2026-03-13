using Ares.Toolkit.Device.UI;
using ReactiveUI.SourceGenerators;
using System.Reactive.Linq;
using ChemyxPumpPlugin.UI.State;

namespace ChemyxPumpPlugin.UI;

public partial class ChemyxPumpUnitControlViewModel : DeviceUnitControlViewModel<ChemyxPump>, IAsyncDisposable
{
  private IDisposable? _stateSubscription;

  public ChemyxPumpUnitControlViewModel(ChemyxPump device) : base(device)
  {
    PumpState = new ChemyxPumpState();
    _stateSubscription = device.StateStream
      .Select(ChemyxPumpStateMapper.FromAresStruct)
      .Subscribe(newState =>
      {
        PumpState = newState;
        HasValidData = true;
      });

    Pump1ViewModel = new IndividualPumpViewModel(1, device);
    Pump2ViewModel = new IndividualPumpViewModel(2, device);

    ViewType = typeof(ChemyxPumpUnitControl);
    DefaultWidth = 40;
  }

  public async ValueTask DisposeAsync()
  {
    _stateSubscription?.Dispose();
    await Pump1ViewModel.DisposeAsync();
    await Pump2ViewModel.DisposeAsync();
  }

  public IndividualPumpViewModel Pump1ViewModel { get; }
  public IndividualPumpViewModel Pump2ViewModel { get; }

  [Reactive]
  public partial ChemyxPumpState PumpState { get; set; }

  [Reactive]
  public partial bool HasValidData { get; private set; }
}
