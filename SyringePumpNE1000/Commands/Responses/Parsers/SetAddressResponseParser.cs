using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ares.SyringePump.Ne1000.Messaging;

namespace SyringePumpNE1000.Commands.Responses.Parsers
{
  internal class SetAddressResponseParser : ResponseParser<SetAddressResponse>
  {
    public SetAddressResponseParser(int address) : base(address)
    {
    }

    protected override bool TryParseResponse(int address, StatusPrompt status, string content, out SetAddressResponse? response)
    {
      if (!int.TryParse(content, out var respondingPumpAddress))
      {
        response = null;
        return false;
      }

      response = new SetAddressResponse(address, status, respondingPumpAddress);
      return true;
    }
  }
}
