using Ares.Device.Serial.Commands;
using GenericSerialDevice.Commands.Responses;
using GenericSerialDevice.Commands.Responses.Parsers;
using System.Text;

namespace GenericSerialDevice.Commands;

public class GetDeviceInfoRequest : SerialCommandWithResponse<GetDeviceInfoResponse>
{
  public GetDeviceInfoRequest() : base(new DeviceInfoResponseParser())
  {

  }

  protected override byte[] Serialize()
  {
    return ASCIIEncoding.ASCII.GetBytes("GET /id HTTP/1.0\r\nHost: any\r\n\r\n");
    //return ASCIIEncoding.ASCII.GetBytes("GET /id HTTP/1.0\r\nHost: any\r\n\r\n");
  }
  
}
