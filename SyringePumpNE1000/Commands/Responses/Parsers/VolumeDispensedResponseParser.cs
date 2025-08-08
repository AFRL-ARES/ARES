using Ares.SyringePump.Ne1000.Messaging;
using UnitsNet;

namespace SyringePumpNE1000.Commands.Responses.Parsers
{
  internal class VolumeDispensedResponseParser : ResponseParser<VolumeDispensedResponse>
  {
    public VolumeDispensedResponseParser(int address) : base(address)
    {
    }

    protected override bool TryParseResponse(int address, StatusPrompt status, string content, out VolumeDispensedResponse? response)
    {
      // TODO: Pick up here
      if (content.Length != "I0.000W0.000UL".Length)
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

      var unit = pumpVolumeUnit switch
      {
        VolumeUnit.UndefinedVolumeUnit => throw new InvalidOperationException(),
        VolumeUnit.Ul => UnitsNet.Units.VolumeUnit.Microliter,
        VolumeUnit.Ml => UnitsNet.Units.VolumeUnit.Milliliter,
        _ => throw new ArgumentOutOfRangeException()
      };

      var infusedStr = content[1..6];
      var withdrawnStr = content[7..^2];
      if (!float.TryParse(infusedStr, out var infusedRaw))
      {
        response = null;
        return false;
      }
      if (!float.TryParse(withdrawnStr, out var withdrawnRaw))
      {
        response = null;
        return false;
      }

      var infused = Volume.From(infusedRaw, unit);
      var withdrawn = Volume.From(withdrawnRaw, unit);

      response = new VolumeDispensedResponse(address, status, infused, withdrawn, pumpVolumeUnit);
      return true;
    }
  }
}
