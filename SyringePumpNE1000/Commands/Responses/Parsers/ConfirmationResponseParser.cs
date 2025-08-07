using Ares.SyringePump.Ne1000.Messaging;

namespace SyringePumpNE1000.Commands.Responses.Parsers
{
  internal class ConfirmationResponseParser : ResponseParser<Response>
  {
    public ConfirmationResponseParser(int address) : base(address)
    {
    }

    protected override bool TryParseResponse(int address, StatusPrompt status, string content, out Response? response)
    {
      if (content.Any())
      {
        response = null;
        return false;
      }
      response = new Response(address, status);
      return true;
    }
  }
}
