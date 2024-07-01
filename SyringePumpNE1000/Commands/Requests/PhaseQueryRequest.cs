using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Ares.Device.Serial.Commands;
using SyringePumpNE1000.Commands.Responses;
using SyringePumpNE1000.Commands.Responses.Parsers;

namespace SyringePumpNE1000.Commands.Requests
{
  internal class PhaseQueryRequest : RequestExpectingResponse<PhaseNumberResponse>
  {
    public PhaseQueryRequest(int address) : base(new PhaseResponseParser(address))
    {
      Address = address;
    }

    protected override string GenerateCommandString()
    {
      var commandStr = $"{Address} {Ares.SyringePump.Ne1000.Messaging.Commands.Phn:G}".ToUpperInvariant();
      return commandStr;
    }
    public int Address { get; }
  }
}
