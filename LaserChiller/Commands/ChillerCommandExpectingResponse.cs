using Ares.Device.Serial.Commands;

namespace LaserChiller.Commands;

public abstract class ChillerCommandExpectingResponse<T> : SerialCommandWithResponse<T> where T : CommandResponse
{
  protected ChillerCommandExpectingResponse(SerialResponseParser<T> parser) : base(parser) { }
}
