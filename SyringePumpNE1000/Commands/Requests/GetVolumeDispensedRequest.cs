using SyringePumpNE1000.Commands.Responses;
using SyringePumpNE1000.Commands.Responses.Parsers;

namespace SyringePumpNE1000.Commands.Requests;

internal class GetVolumeDispensedRequest : RequestExpectingResponse<VolumeDispensedResponse>
{
  public GetVolumeDispensedRequest(int address) : base(new VolumeDispensedResponseParser(address))
  {
    Address = address;
  }

  protected override string GenerateCommandString()
  {
    var commandStr = $"{Address} {Ares.SyringePump.Ne1000.Messaging.Commands.Dis}".ToUpperInvariant();
    return commandStr;
  }
  public int Address { get; }
}
