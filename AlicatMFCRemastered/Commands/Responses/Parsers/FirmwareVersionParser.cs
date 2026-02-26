using Ares.Toolkit.Serial.Commands;
using System.Text.RegularExpressions;

namespace AlicatMFCRemastered.Commands.Responses.Parsers;

internal class FirmwareVersionParser : AsciiResponseParser<FirmwareVersionResponse>
{
  private readonly Regex _firmwareIdentifier = new Regex(@"\dv\d+");
  private readonly char _assumedId;

  public FirmwareVersionParser(char assumedId)
  {
    _assumedId = assumedId;
  }

  protected override bool TryParseResponse(string line, out FirmwareVersionResponse? response)
  {
    var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if(tokens[0].First() != _assumedId)
    {
      response = null;
      return false;
    }

    var firmwareToken = tokens[1];
    var firmwareMatch = _firmwareIdentifier.IsMatch(firmwareToken);
    if(firmwareMatch || firmwareToken.StartsWith("GP", StringComparison.InvariantCultureIgnoreCase))
    {
      response = new FirmwareVersionResponse(_assumedId, firmwareToken);
      return true;
    }

    response = null;
    return false;
  }
}
