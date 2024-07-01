using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ares.Device.Serial.Commands;
using Ares.SyringePump.Ne1000.Messaging;
using SyringePumpNE1000.Commands.Responses;
using SyringePumpNE1000.Commands.Responses.Parsers;
using UnitsNet;

namespace SyringePumpNE1000.Commands.Requests
{
  internal class SetPhaseFunctionRateRequest : RequestExpectingResponse<Response>
  {

    public SetPhaseFunctionRateRequest(int address, Speed rate) : base(new ConfirmationResponseParser(address))
    {
      Address = address;
      Rate = rate;
    }

    protected override string GenerateCommandString()
    {
      var floatStr = FormatHelper.FormatToFloatString(Rate.MillimetersPerMinutes);
      var commandData = $"{Address} {Ares.SyringePump.Ne1000.Messaging.Commands.Rat} C {floatStr} {RateUnit.Mm}".ToUpperInvariant();
      return commandData;
    }

    public int Address { get; }
    public Speed Rate { get; }
  }
}
