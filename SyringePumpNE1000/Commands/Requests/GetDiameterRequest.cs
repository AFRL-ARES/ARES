using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ares.Device.Serial.Commands;
using SyringePumpNE1000.Commands.Responses;
using SyringePumpNE1000.Commands.Responses.Parsers;

namespace SyringePumpNE1000.Commands.Requests;

  internal class GetDiameterRequest : RequestExpectingResponse<DiameterResponse>
  {

    public GetDiameterRequest(int address) : base(new DiameterResponseParser(address), false)
    {
      Address = address;
    }

    protected override string GenerateCommandString()
  {
    var commandStr = $"{Address} {Ares.SyringePump.Ne1000.Messaging.Commands.Dia:G}".ToUpperInvariant();
    return commandStr;

  }
  public int Address { get; }
  }
