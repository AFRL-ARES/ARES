using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ares.SyringePump.Ne1000.Messaging;

namespace SyringePumpNE1000.Commands.Responses.Parsers
{
  internal class AddressQueryResponseParser : ResponseParser<AddressQueryResponse>
  {
    public AddressQueryResponseParser(int address) : base(address)
    {
    }

    protected override bool TryParseResponse(int address, StatusPrompt status, string content, out AddressQueryResponse? response)
    {
      // TODO: Test this with multiple syringe pumps on the network.
      // Single pump with Address 0 returns 00S00
      // Would several pumps return something like 00S00S01S02S03?
      if (content.Length != "##".Length)
      {
        response = null;
        return false;
      }

      if (!int.TryParse(content, out var respondingPumpAddress))
      {
        response = null;
        return false;
      }

      response = new AddressQueryResponse(address, status, respondingPumpAddress);
      return true;
    }
  }
}
