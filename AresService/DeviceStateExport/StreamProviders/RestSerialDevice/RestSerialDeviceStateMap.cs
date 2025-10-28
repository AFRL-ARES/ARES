using Ares.Messages.DeviceStates.RestSerialDevice;
using CsvHelper.Configuration;

namespace AresService.DeviceStateExport.StreamProviders.RestSerialDevice;

public class RestSerialDeviceStateMap : ClassMap<RestSerialDeviceStateEntity>
{
  public RestSerialDeviceStateMap()
  {
    Map(rd => rd.Timestamp).Index(0).Name("Timestamp");
  }
}
