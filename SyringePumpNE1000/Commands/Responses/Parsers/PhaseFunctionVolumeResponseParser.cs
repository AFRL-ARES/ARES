using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ares.SyringePump.Ne1000.Messaging;
using UnitsNet;

namespace SyringePumpNE1000.Commands.Responses.Parsers
{
  internal class PhaseFunctionVolumeResponseParser : ResponseParser<PhaseFunctionVolumeResponse>
  {
    public PhaseFunctionVolumeResponseParser(int address) : base(address)
    {
    }

    protected override bool TryParseResponse(int address, StatusPrompt status, string content, out PhaseFunctionVolumeResponse? response)
    {
      if (content.Length != "0.000UL".Length)
      {
        response = null;
        return false;
      }

      var unitStr = content[^2..];
      if (!Enum.TryParse<VolumeUnit>(unitStr, true, out var pumpVolumeUnit))
      {
        response = null;
        return false;
      }

      var unit = pumpVolumeUnit.ToUnitsNet();

      var volumeStr = content[..^2];
      if (!float.TryParse(volumeStr, out var volumeRaw))
      {
        response = null;
        return false;
      }
      
      var volume = Volume.From(volumeRaw, unit);
      response = new(address, status, volume, pumpVolumeUnit);
      return true;
    }
  }
}
