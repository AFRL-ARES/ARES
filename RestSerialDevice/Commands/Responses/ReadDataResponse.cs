using Ares.Device.Serial.Commands;

namespace GenericSerialDevice.Commands.Responses;

public class ReadDataResponse : SerialResponse
{
  public ReadDataResponse(Dictionary<string, string> values)
  {
    Values = values;
  }

  public Dictionary<string, string> Values { get; set; }
}
