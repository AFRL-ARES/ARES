using Ares.Toolkit.Serial.Commands;

namespace AlicatMFCRemastered.Commands.Responses;

public abstract class CommandResponse : SerialResponse
{
  public CommandResponse(char id)
  {
    Id = id;
  }

  public char Id { get; }
}
