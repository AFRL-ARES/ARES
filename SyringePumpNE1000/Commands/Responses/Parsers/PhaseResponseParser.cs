using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ares.SyringePump.Ne1000.Messaging;

namespace SyringePumpNE1000.Commands.Responses.Parsers
{
  internal class PhaseResponseParser : ResponseParser<PhaseNumberResponse>
  {
    public PhaseResponseParser(int address) : base(address)
    {
    }

    protected override bool TryParseResponse(int address, StatusPrompt status, string content, out PhaseNumberResponse? response)
    {
      if (!int.TryParse(content, out var phase))
      {
        response = null;
        return false;
      }

      response = new PhaseNumberResponse(address, status, phase);
      return true;
    }
  }
}
