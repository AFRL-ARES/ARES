using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ares.Device.Serial.Commands;
using Ares.SyringePump.Ne1000.Messaging;
using SyringePumpNE1000.Commands.Responses;
using SyringePumpNE1000.Commands.Responses.Parsers;

namespace SyringePumpNE1000.Commands.Requests
{
  internal class ClearVolumeRequest : RequestExpectingResponse<Response>
  {
    public ClearVolumeRequest(int address, Direction direction) : base(new ConfirmationResponseParser(address))
    {
      Address = address;
      Direction = direction;
    }

    protected override string GenerateCommandString()
    {
      var commandData = $"{Address} {Ares.SyringePump.Ne1000.Messaging.Commands.Cld} {Direction}".ToUpperInvariant();
      return commandData;
    }
    public int Address { get; }
    public Direction Direction { get; }
  }
}
