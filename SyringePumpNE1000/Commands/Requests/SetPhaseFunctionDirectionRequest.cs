using Ares.SyringePump.Ne1000.Messaging;
using SyringePumpNE1000.Commands.Responses;
using SyringePumpNE1000.Commands.Responses.Parsers;

namespace SyringePumpNE1000.Commands.Requests;

internal class SetPhaseFunctionDirectionRequest : RequestExpectingResponse<Response>
{

  public SetPhaseFunctionDirectionRequest(int address, Direction direction) : base(new ConfirmationResponseParser(address))
  {
    Address = address;
    Direction = direction;
  }

  protected override string GenerateCommandString()
  {
    var commandStr = $"{Address} {Ares.SyringePump.Ne1000.Messaging.Commands.Dir:G} {Direction:G}".ToUpperInvariant();
    return commandStr;
  }
  public int Address { get; }
  public Direction Direction { get; }
}
