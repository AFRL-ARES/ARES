using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ares.SyringePump.Ne1000.Messaging;

namespace SyringePumpNE1000.Commands.Responses.Parsers
{
  internal class PhaseFunctionResponseParser : ResponseParser<PhaseFunctionResponse>
  {
    public PhaseFunctionResponseParser(int address) : base(address)
    {
    }

    protected override bool TryParseResponse(int address, StatusPrompt status, string content, out PhaseFunctionResponse? response)
    {
      if (!Enum.TryParse<Ares.SyringePump.Ne1000.Messaging.Commands>(content, true, out var function))
      {
        response = null;
        return false;
      }

      response = new PhaseFunctionResponse(address, status, function);
      return true;
    }
  }
}
