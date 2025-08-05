using Ares.Device.Serial.Commands;

namespace HerkulexDRS.Responses;

public abstract class CommandResponse : SerialResponse
{
  public CommandResponse(int servoId)
  {
    ServoId = servoId;
  }

  public int ServoId { get; set; }
}
