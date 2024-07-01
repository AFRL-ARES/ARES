
using Ares.Device.Serial.Commands;
using Ares.SyringePump.Ne1000.Messaging;
using UnitsNet;

namespace SyringePumpNE1000.Commands.Responses.Parsers
{
  internal class DiameterResponseParser : ResponseParser<DiameterResponse>
  {
    public DiameterResponseParser(int address) : base(address)
    {
    }

    protected override bool TryParseResponse(int address, StatusPrompt status, string content, out DiameterResponse? response)
    {
      if (!float.TryParse(content, out var diameterMm))
      {
        response = null;
        return false;
      }

      response = new DiameterResponse(address, status, Length.FromMillimeters(diameterMm));
      return true;
    }
  }
}
