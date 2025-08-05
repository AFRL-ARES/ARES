using Ares.Device.Serial.Commands;
using Ares.SyringePump.Ne1000.Messaging;

namespace SyringePumpNE1000.Commands.Responses;

public class Response : SerialResponse
{
  public Response(int address, StatusPrompt status, CommandError? error)
  {
    Address = address;
    Status = status;
    Error = error;
  }

  public Response(int address, StatusPrompt status) : this(address, status, null)
  {
  }

  public int Address { get; }
  public StatusPrompt Status { get; }
  public CommandError? Error { get; }
}
