using Ares.Device.Serial.Commands;
using RestSerialDevice.Commands.Responses;
using RestSerialDevice.Commands.Responses.Parsers;
using System.Text;

namespace RestSerialDevice.Commands;

public class GetDeviceCapabilitiesRequest : SerialCommandWithResponse<GetDeviceCapabilitiesResponse>
{
  public GetDeviceCapabilitiesRequest() : base(new DeviceServicesResponseParser())
  {

  }

  protected override byte[] Serialize()
  {
    return ASCIIEncoding.ASCII.GetBytes("GET /capabilities HTTP/1.0\r\nHost: any\r\n\r\n");
  }

}
