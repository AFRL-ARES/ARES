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
  internal class StartRequest : RequestExpectingResponse<Response>
  {
    public StartRequest(int address) : base(new ConfirmationResponseParser(address))
    {
      Address = address;
    }

    protected override string GenerateCommandString()
    {
      // TODO: Consider the 'E' optional argument (trigger-related)?
      var commandData = $"{Address} {Ares.SyringePump.Ne1000.Messaging.Commands.Run}".ToUpperInvariant();
      return commandData;
    }
    public int Address { get; }
  }
}
