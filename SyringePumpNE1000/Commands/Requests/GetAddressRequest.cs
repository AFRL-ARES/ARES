using SyringePumpNE1000.Commands.Responses;
using SyringePumpNE1000.Commands.Responses.Parsers;

namespace SyringePumpNE1000.Commands.Requests;

internal class GetAddressRequest : RequestExpectingResponse<AddressQueryResponse>
{
  public GetAddressRequest(int address) : base(new AddressQueryResponseParser(address), false)
  {
  }

  protected override string GenerateCommandString()
  {
    return $"*{Ares.SyringePump.Ne1000.Messaging.Commands.Adr}".ToUpperInvariant();
  }
}
