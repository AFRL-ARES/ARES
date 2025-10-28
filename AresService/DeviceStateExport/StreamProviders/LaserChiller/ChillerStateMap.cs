using Ares.Messages.DeviceStates.Chiller;
using CsvHelper.Configuration;

namespace AresService.DeviceStateExport.StreamProviders.LaserChiller;

public class ChillerStateMap : ClassMap<ChillerState>
{
  public ChillerStateMap()
  {
    Map(m => m.Timestamp).Index(0).Name("Timestamp");
    Map(m => m.ManifoldTemperature).Index(1).Name("Manifold Temperature");
  }
}
