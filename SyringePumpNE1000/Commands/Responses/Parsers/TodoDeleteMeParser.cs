using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ares.SyringePump.Ne1000.Messaging;

namespace SyringePumpNE1000.Commands.Responses.Parsers
{
  // TODO: Delete me
  internal class TodoDeleteMeParser : ResponseParser<IgnorableResponse>
  {
    public TodoDeleteMeParser(int address) : base(address)
    {
    }

    protected override bool TryParseResponse(int address, StatusPrompt status, string content, out IgnorableResponse? response)
    {
      response = new IgnorableResponse(address, status, content);
      return true;
    }
  }
}
