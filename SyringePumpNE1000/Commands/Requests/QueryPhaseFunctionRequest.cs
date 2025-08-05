using SyringePumpNE1000.Commands.Responses;
using SyringePumpNE1000.Commands.Responses.Parsers;

namespace SyringePumpNE1000.Commands.Requests;

internal class QueryPhaseFunctionRequest : RequestExpectingResponse<PhaseFunctionResponse>
{
  public QueryPhaseFunctionRequest(int address) : base(new PhaseFunctionResponseParser(address))
  {
    Address = address;
  }

  protected override string GenerateCommandString()
  {
    var commandData = $"{Address} {Ares.SyringePump.Ne1000.Messaging.Commands.Fun}".ToUpperInvariant();
    return commandData;
  }
  public int Address { get; }
}
