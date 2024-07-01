using Ares.Messages.DeviceStates.Tc0304;
using CsvHelper.Configuration;

namespace UI.Backend.DeviceStateExport.StreamProviders.Tc0304;

public class Tc0304StateMap : ClassMap<Tc0304State>
{
  public Tc0304StateMap()
  {
    Map(m => m.Timestamp).Index(0).Name("Timestamp");
    Map(m => m.Probe1Temperature).Index(1).Name("Probe 1 Temperature (°C)");
    Map(m => m.Probe2Temperature).Index(2).Name("Probe 2 Temperature (°C)");
    Map(m => m.Probe3Temperature).Index(3).Name("Probe 3 Temperature (°C)");
    Map(m => m.Probe4Temperature).Index(4).Name("Probe 4 Temperature (°C)");
  }
}
