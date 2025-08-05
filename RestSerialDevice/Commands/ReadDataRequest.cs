using Ares.Device.Serial.Commands;
using GenericSerialDevice.Commands.Responses;
using GenericSerialDevice.Commands.Responses.Parsers;
using System.Text;

namespace GenericSerialDevice.Commands;

public class ReadDataRequest : SerialCommandWithResponse<ReadDataResponse>
{
  public ReadDataRequest() : base(new ReadDataResponseParser())
  {
  }

  protected override byte[] Serialize()
  {
    return Encoding.ASCII.GetBytes("GET /variables HTTP/1.0\r\nHost: any\r\n\r\n");
  }
}
