using Ares.Messages.DeviceStates.TicStepperController;
using UI.Backend.DeviceStateExport.StateGetters;

namespace UI.Backend.DeviceStateExport.ExportDataProviders.Devices;

public class TicStepperControllerExportDataProvider : DeviceStateDataProviderBase<TicStepperControllerState>
{
  public TicStepperControllerExportDataProvider(IDeviceStateGetter<TicStepperControllerState> stateGetter) : base(stateGetter)
  {
  }

  protected override IEnumerable<StateExportLine> GetExportLines(string deviceName, IEnumerable<TicStepperControllerState> deviceStates)
  {
    var exportItems = deviceStates.Select(d =>
    {
      var itemsAtUniqueTimestamp = new StateExportItem[]
      {
        new StateExportItem("Max Acceleration (microsteps per 100 s²)", deviceName, d.MaxAcceleration),
        new StateExportItem("Max Deceleration (microsteps per 100 s²)", deviceName, d.MaxDeceleration),
        new StateExportItem("Max Speed (microsteps per 10,000 s)", deviceName, d.MaxSpeed),
        new StateExportItem("Starting Speed (microsteps per 10,000 ²)", deviceName, d.StartingSpeed),
        new StateExportItem("Custom Step Size (user defined steps)", deviceName, d.CustomStepSize),
        new StateExportItem("Step Mode", deviceName, d.StepMode),
        new StateExportItem("Current Position (microsteps)", deviceName, d.CurrentPosition),
        new StateExportItem("Target Position (microsteps)", deviceName, d.TargetPosition),
        new StateExportItem("Current Statuses", deviceName, d.StatusMessages)
      };

      return new StateExportLine(itemsAtUniqueTimestamp, d.Timestamp.ToDateTime(), deviceName);
    });

    return exportItems;
  }
}
