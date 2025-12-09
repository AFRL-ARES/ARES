using Ares.Device.Serial.Commands;
using ChemyxPumpPlugin.Commands.Parsing;

namespace ChemyxPumpPlugin.Commands.Responses;

internal class ChemyxPumpResponseParser : SerialResponseParser<ChemyxPumpResponse>
{
  public override bool TryParseResponse(byte[] buffer, out ChemyxPumpResponse? response, out ArraySegment<byte>? dataToRemove)
    => ChemyxPumpParsing.TryParse(buffer, out response!, out dataToRemove);
}
