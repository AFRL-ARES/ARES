using Ares.Messages.DeviceStates.TubeFurnace;
using CsvHelper.Configuration;

namespace UI.Backend.DeviceStateExport.StreamProviders.TubeFurnace;

public class TubeFurnaceStateMap : ClassMap<TubeFurnaceStateEntity>
{
  public TubeFurnaceStateMap()
  {
    Map(tf => tf.Timestamp).Index(0).Name("Timestamp");
  }
}
