using Ares.SyringePump.Ne1000.Messaging;

namespace SyringePumpNE1000.Commands.Responses;

internal class SetAddressResponse : Response
{
  public SetAddressResponse(int address, StatusPrompt status, CommandError? error) : base(address, status, error)
  {
  }
  public SetAddressResponse(int address, StatusPrompt status, int respondingAddress) : base(address, status)
  {
    RespondingAddress = respondingAddress;
  }

  public int RespondingAddress { get; }

}
