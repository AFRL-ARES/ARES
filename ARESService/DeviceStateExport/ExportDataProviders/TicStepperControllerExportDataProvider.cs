using Ares.Core.Device.State.Export;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Messages.DeviceStates.TicStepperController;
using System.Collections.Generic;
using System.Linq;

namespace AresService.DeviceStateExport.ExportDataProviders;

public class TicStepperControllerExportDataProvider : DeviceStateDataProviderBase<TicStepperControllerState>
{
  public TicStepperControllerExportDataProvider(IDeviceStateGetter stateGetter) : base(
    stateGetter)
  {
  }

  protected override IEnumerable<StateExportLine> GetExportLines(
    string deviceName,
    IEnumerable<TicStepperControllerState> deviceStates)
  {
    var exportItems = deviceStates.Select(
      d =>
      {
        var itemsAtUniqueTimestamp = new StateExportItem[]
        {
          new("Max Acceleration (microsteps per 100 s²)", deviceName, d.MaxAcceleration),
          new("Max Deceleration (microsteps per 100 s²)", deviceName, d.MaxDeceleration),
          new("Max Speed (microsteps per 10,000 s)", deviceName, d.MaxSpeed),
          new("Starting Speed (microsteps per 10,000 ²)", deviceName, d.StartingSpeed),
          new("Custom Step Size (user defined steps)", deviceName, d.CustomStepSize),
          new("Step Mode", deviceName, d.StepMode),
          new("Current Position (microsteps)", deviceName, d.CurrentPosition),
          new("Target Position (microsteps)", deviceName, d.TargetPosition),
          new("Current Statuses", deviceName, d.StatusMessages)
        };

        return new StateExportLine(itemsAtUniqueTimestamp, d.Timestamp.ToDateTime(), deviceName);
      });

    return exportItems;
  }
}
