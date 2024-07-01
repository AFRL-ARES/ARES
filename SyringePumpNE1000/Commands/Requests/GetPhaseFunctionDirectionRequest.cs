using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ares.Device.Serial.Commands;
using SyringePumpNE1000.Commands.Responses;
using SyringePumpNE1000.Commands.Responses.Parsers;

namespace SyringePumpNE1000.Commands.Requests
{
  internal class GetPhaseFunctionDirectionRequest : RequestExpectingResponse<PhaseFunctionDirectionResponse>
  {
    public int Address { get; }

    public GetPhaseFunctionDirectionRequest(int address) : base(new PhaseFunctionDirectionResponseParser(address))
    {
      Address = address;
    }

    protected override string GenerateCommandString()
    {
      var commandStr = $"{Address} {Ares.SyringePump.Ne1000.Messaging.Commands.Dir:G}".ToUpperInvariant();
      return commandStr;
    }
  }
}
