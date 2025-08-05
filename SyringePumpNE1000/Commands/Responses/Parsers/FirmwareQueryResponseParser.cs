using Ares.SyringePump.Ne1000.Messaging;

namespace SyringePumpNE1000.Commands.Responses.Parsers;

internal class FirmwareQueryResponseParser : ResponseParser<FirmwareQueryResponse>
{
  public FirmwareQueryResponseParser(int address) : base(address)
  {
  }

  protected override bool TryParseResponse(int address, StatusPrompt status, string content, out FirmwareQueryResponse? response)
  {
    response = new FirmwareQueryResponse(address, status, content);
    return true;
  }
}
