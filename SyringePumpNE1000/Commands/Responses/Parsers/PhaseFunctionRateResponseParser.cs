using Ares.SyringePump.Ne1000.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;
using UnitsNet.Units;

namespace SyringePumpNE1000.Commands.Responses.Parsers
{
  internal class PhaseFunctionRateResponseParser : ResponseParser<PhaseFunctionRateResponse>
  {
    public PhaseFunctionRateResponseParser(int address) : base(address)
    {
    }

    protected override bool TryParseResponse(int address, StatusPrompt status, string content, out PhaseFunctionRateResponse? response)
    {
      var unitStr = content[^2..];
      var floatStr = content[..^2];
      if(!Enum.TryParse<RateUnit>(unitStr, true, out var pumpRateUnit))
      {
        response = null;
        return false;
      }
      if(!float.TryParse(floatStr, out var rateInPumpUnit))
      {
        response = null;
        return false;
      }

      var unit = pumpRateUnit.ToUnitsNet();
      var rate = Speed.From(rateInPumpUnit, unit);

      response = new PhaseFunctionRateResponse(address, status, rate, pumpRateUnit);
      return true;
    }
  }
}
