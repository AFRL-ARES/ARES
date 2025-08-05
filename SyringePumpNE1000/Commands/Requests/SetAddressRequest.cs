using SyringePumpNE1000.Commands.Responses;
using SyringePumpNE1000.Commands.Responses.Parsers;

namespace SyringePumpNE1000.Commands.Requests;

internal class SetAddressRequest : RequestExpectingResponse<Response>
{
  public SetAddressRequest(int address) : base(new ConfirmationResponseParser(address))
  {
    Address = address;
  }

  protected override string GenerateCommandString()
  {
    return $"*{Ares.SyringePump.Ne1000.Messaging.Commands.Adr} {Address}".ToUpperInvariant();
  }

  public int Address { get; }
}
