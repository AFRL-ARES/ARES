using Ares.Messages.DeviceStates.RestDevice;
using CsvHelper.Configuration;

namespace AresService.DeviceStateExport.StreamProviders.RestDevice;

public class RestDeviceStateMap : ClassMap<RestDeviceStateEntity>
{
  public RestDeviceStateMap()
  {
    Map(rd => rd.Timestamp).Index(0).Name("Timestamp");
  }
}
