using Ares.Messages.DeviceStates.TicStepperController;
using CsvHelper.Configuration;

namespace UI.Backend.DeviceStateExport.StreamProviders.StepperController;

public class TicStepperControllerStateMap : ClassMap<TicStepperControllerState>
{
  public TicStepperControllerStateMap()
  {
    Map(tsc => tsc.Timestamp).Index(0).Name("Timestamp");
    Map(tsc => tsc.MaxAcceleration).Index(1).Name("Max Acceleration (microsteps per 100 s²)");
    Map(tsc => tsc.MaxDeceleration).Index(2).Name("Max Deceleration (microsteps per 100 s²)");
    Map(tsc => tsc.MaxSpeed).Index(3).Name("Max Speed (microsteps per 10,000 s)");
    Map(tsc => tsc.StartingSpeed).Index(4).Name("Starting Speed (microsteps per 10,000 ²)");
    Map(tsc => tsc.CustomStepSize).Index(5).Name("Custom Step Size (user defined steps)");
    Map(tsc => tsc.StepMode).Index(6).Name("Step Mode");
    Map(tsc => tsc.CurrentPosition).Index(7).Name("Current Position (microsteps)");
    Map(tsc => tsc.TargetPosition).Index(8).Name("Target Position (microsteps)");
    Map(tsc => tsc.StatusMessages).Index(9).Name("Current Statuses");
  }
}
