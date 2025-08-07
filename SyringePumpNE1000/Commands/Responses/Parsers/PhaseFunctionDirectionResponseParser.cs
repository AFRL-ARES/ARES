using Ares.SyringePump.Ne1000.Messaging;

namespace SyringePumpNE1000.Commands.Responses.Parsers
{
  internal class PhaseFunctionDirectionResponseParser : ResponseParser<PhaseFunctionDirectionResponse>
  {
    public PhaseFunctionDirectionResponseParser(int address) : base(address)
    {
    }

    protected override bool TryParseResponse(int address, StatusPrompt status, string content, out PhaseFunctionDirectionResponse? response)
    {
      if (!Enum.TryParse<Direction>(content, true, out var direction))
      {
        response = null;
        return false;
      }

      response = new PhaseFunctionDirectionResponse(address, status, direction);
      return true;
    }
  }
}
