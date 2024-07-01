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
  internal class GetAddressRequest : RequestExpectingResponse<AddressQueryResponse>
  {
    public GetAddressRequest() : base(new AddressQueryResponseParser(0xDead), false)
    {
    }

    protected override string GenerateCommandString()
    {
      return $"*{Ares.SyringePump.Ne1000.Messaging.Commands.Adr}".ToUpperInvariant();
    }
  }
}
