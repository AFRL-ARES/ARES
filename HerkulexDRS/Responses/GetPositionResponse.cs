using Ares.Device.Serial.Commands;

namespace HerkulexDRS.Responses;
public class GetPositionResponse : SerialResponse
{
  public double Position { get; init; }
}
