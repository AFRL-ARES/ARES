using ChemyxPumpPlugin.Enums;
using ChemyxPumpPlugin.UI.State;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive.Linq;
using Ares.Datamodel;
using Ares.Datamodel.Device;

namespace ChemyxPumpPlugin.UI;

public partial class IndividualPumpViewModel : ReactiveObject, IAsyncDisposable
{
  private readonly int _pumpNumber;
  private readonly ChemyxPump _device;
  private IDisposable? _stateSubscription;

  public IndividualPumpViewModel(int pumpNumber, ChemyxPump device)
  {
    _pumpNumber = pumpNumber;
    _device = device;

    _stateSubscription = device.StateStream
      .Select(ChemyxPumpStateMapper.FromAresStruct)
      .Subscribe(newState =>
      {
        var pumpData = newState.Pumps.FirstOrDefault(p => p.Index == _pumpNumber);
        if(pumpData != null)
        {
          Status = Enum.TryParse<PumpStatus>(pumpData.Status, out var s) ? s : PumpStatus.Stopped;
          Dispensed = pumpData.Volume;
          Elapsed = TimeSpan.TryParse(pumpData.Time, out var t) ? t : TimeSpan.Zero;
          Rate = pumpData.Rate;
          Volume = pumpData.TargetVolume;
          Delay = TimeSpan.FromSeconds(pumpData.Delay);
          PumpUnit = Enum.TryParse<PumpUnits>(pumpData.Units, out var u) ? u : PumpUnits.MillilitersPerMinute;
          Diameter = pumpData.Diameter;
          HasValidData = true;
        }
      });
  }

  public Task StartPump() => _device.ExecuteCommand(ChemyxPumpCommand.Start.ToString(), [new DeviceCommandArgument { ArgName = "PumpIndex", ArgValue = new AresValue { NumberValue = _pumpNumber } }], default);

  public Task StopPump() => _device.ExecuteCommand(ChemyxPumpCommand.Stop.ToString(), [new DeviceCommandArgument { ArgName = "PumpIndex", ArgValue = new AresValue { NumberValue = _pumpNumber } }], default);

  public Task PausePump() => _device.ExecuteCommand(ChemyxPumpCommand.Pause.ToString(), [new DeviceCommandArgument { ArgName = "PumpIndex", ArgValue = new AresValue { NumberValue = _pumpNumber } }], default);

  public Task SetRate(double rate) => _device.ExecuteCommand(ChemyxPumpCommand.SetRate.ToString(), [
      new DeviceCommandArgument { ArgName = "PumpIndex", ArgValue = new AresValue { NumberValue = _pumpNumber } },
      new DeviceCommandArgument { ArgName = "Rate", ArgValue = new AresValue { NumberValue = rate } }
    ], default);

  public Task SetVolume(double volume) => _device.ExecuteCommand(ChemyxPumpCommand.SetVolume.ToString(), [
      new DeviceCommandArgument { ArgName = "PumpIndex", ArgValue = new AresValue { NumberValue = _pumpNumber } },
      new DeviceCommandArgument { ArgName = "Volume", ArgValue = new AresValue { NumberValue = volume } }
    ], default);

  public Task SetUnit(PumpUnits unit) => _device.ExecuteCommand(ChemyxPumpCommand.SetUnits.ToString(), [
      new DeviceCommandArgument { ArgName = "PumpIndex", ArgValue = new AresValue { NumberValue = _pumpNumber } },
      new DeviceCommandArgument { ArgName = "Units", ArgValue = new AresValue { NumberValue = (int)unit } }
    ], default);

  public Task SetDelay(TimeSpan delay) => _device.ExecuteCommand(ChemyxPumpCommand.SetDelay.ToString(), [
      new DeviceCommandArgument { ArgName = "PumpIndex", ArgValue = new AresValue { NumberValue = _pumpNumber } },
      new DeviceCommandArgument { ArgName = "Delay", ArgValue = new AresValue { NumberValue = delay.TotalSeconds } }
    ], default);

  public Task SetTime(TimeSpan time) => _device.ExecuteCommand(ChemyxPumpCommand.SetTime.ToString(), [
      new DeviceCommandArgument { ArgName = "PumpIndex", ArgValue = new AresValue { NumberValue = _pumpNumber } },
      new DeviceCommandArgument { ArgName = "Time", ArgValue = new AresValue { NumberValue = time.TotalMinutes } }
    ], default);

  public Task SetDiameter(double diameter) => _device.ExecuteCommand(ChemyxPumpCommand.SetDiameter.ToString(), [
      new DeviceCommandArgument { ArgName = "PumpIndex", ArgValue = new AresValue { NumberValue = _pumpNumber } },
      new DeviceCommandArgument { ArgName = "Diameter", ArgValue = new AresValue { NumberValue = diameter } }
    ], default);

  public ValueTask DisposeAsync()
  {
    _stateSubscription?.Dispose();
    return ValueTask.CompletedTask;
  }

  public int PumpNumber => _pumpNumber;
  public static IEnumerable<string> AvailableUnits => Enum.GetNames(typeof(PumpUnits));
  public string UniqueId => $"{_device.UniqueId}-{_pumpNumber}";

  [Reactive]
  public partial bool HasValidData { get; private set; }

  [Reactive]
  public partial PumpStatus Status { get; set; }

  [Reactive]
  public partial double Dispensed { get; set; }

  [Reactive]
  public partial TimeSpan Elapsed { get; set; }

  [Reactive]
  public partial double Rate { get; set; }

  [Reactive]
  public partial double Volume { get; set; }

  [Reactive]
  public partial TimeSpan Delay { get; set; }

  [Reactive]
  public partial TimeSpan Time { get; set; }

  [Reactive]
  public partial PumpUnits PumpUnit { get; set; }

  [Reactive]
  public partial double Diameter { get; set; }
}
