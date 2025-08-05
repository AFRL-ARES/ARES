using SyringePumpNE1000.Commands.Responses;
using SyringePumpNE1000.Commands.Responses.Parsers;

namespace SyringePumpNE1000.Commands.Requests;

internal class GetFirmwareVersionRequest : RequestExpectingResponse<FirmwareQueryResponse>
{
  public GetFirmwareVersionRequest(int address) : base(new FirmwareQueryResponseParser(address), false)
  {
    Address = address;
  }

  protected override string GenerateCommandString()
  {
    return $"{Address} {Ares.SyringePump.Ne1000.Messaging.Commands.Ver}".ToUpperInvariant();
  }

  public int Address { get; }
}
