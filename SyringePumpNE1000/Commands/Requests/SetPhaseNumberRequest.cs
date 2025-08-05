using SyringePumpNE1000.Commands.Responses;
using SyringePumpNE1000.Commands.Responses.Parsers;

namespace SyringePumpNE1000.Commands.Requests;

internal class SetPhaseNumberRequest : RequestExpectingResponse<Response>
{

  public SetPhaseNumberRequest(int address, int phase) : base(new ConfirmationResponseParser(address))
  {
    Address = address;
    Phase = phase;
  }

  protected override string GenerateCommandString()
  {
    var commandStr = $"{Address} {Ares.SyringePump.Ne1000.Messaging.Commands.Phn:G} {Phase}".ToUpperInvariant();
    return commandStr;
  }
  public int Address { get; }
  public int Phase { get; }
}
