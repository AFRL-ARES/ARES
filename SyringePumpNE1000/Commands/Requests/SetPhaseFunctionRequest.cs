using SyringePumpNE1000.Commands.Responses;
using SyringePumpNE1000.Commands.Responses.Parsers;

namespace SyringePumpNE1000.Commands.Requests;

internal class SetPhaseFunctionRequest : RequestExpectingResponse<Response>
{

  public SetPhaseFunctionRequest(int address, Ares.SyringePump.Ne1000.Messaging.Commands function) : base(new ConfirmationResponseParser(address))
  {
    Address = address;
    Function = function;
  }

  protected override string GenerateCommandString()
  {
    var commandStr = $"{Address} {Ares.SyringePump.Ne1000.Messaging.Commands.Fun:G} {Function:G}".ToUpperInvariant();
    return commandStr;
  }
  public int Address { get; }
  public Ares.SyringePump.Ne1000.Messaging.Commands Function { get; }
}
