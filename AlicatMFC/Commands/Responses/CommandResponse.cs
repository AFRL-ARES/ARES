using Ares.Device.Serial.Commands;

namespace AlicatMFC.Commands.Responses;

public abstract class CommandResponse : SerialResponse
{
  public CommandResponse(char id)
  {
    Id = id;
  }

  public char Id { get; }
}
