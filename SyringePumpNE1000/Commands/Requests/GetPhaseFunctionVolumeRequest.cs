using SyringePumpNE1000.Commands.Responses;
using SyringePumpNE1000.Commands.Responses.Parsers;

namespace SyringePumpNE1000.Commands.Requests;

internal class GetPhaseFunctionVolumeRequest : RequestExpectingResponse<PhaseFunctionVolumeResponse>
{
  public int Address { get; }

  public GetPhaseFunctionVolumeRequest(int address) : base(new PhaseFunctionVolumeResponseParser(address))
  {
    Address = address;
  }

  protected override string GenerateCommandString()
  {
    var commandStr = $"{Address} {Ares.SyringePump.Ne1000.Messaging.Commands.Vol:G}".ToUpperInvariant();
    return commandStr;
  }
}
