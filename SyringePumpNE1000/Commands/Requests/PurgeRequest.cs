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
  internal class PurgeRequest : RequestExpectingResponse<Response>
  {
    public PurgeRequest(int address) : base(new ConfirmationResponseParser(address))
    {
      Address = address;
    }

    protected override string GenerateCommandString()
    {
      var commandData = $"{Address} {Ares.SyringePump.Ne1000.Messaging.Commands.Pur}".ToUpperInvariant();
      return commandData;
    }
    public int Address { get; }
  }
}