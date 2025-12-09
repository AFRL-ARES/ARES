using System.Globalization;
using Ares.Device.Serial.Commands;
using ChemyxPumpPlugin.Commands.Parsing;

namespace ChemyxPumpPlugin.Commands.Responses;

internal class SetTimeResponseParser : SerialResponseParser<SetTimeResponse>
{
  public override bool TryParseResponse(byte[] buffer, out SetTimeResponse? response, out ArraySegment<byte>? dataToRemove)
  {
    response = null;
    if(!ChemyxPumpParsing.TryParse(buffer, out var baseResponse, out dataToRemove))
      return false;

    double? rate = null;
    double? time = null;

    foreach(var line in baseResponse.ResponseLines)
    {
      if(line.Contains("rate", StringComparison.InvariantCultureIgnoreCase))
      {
        var valueString = line.Split('=', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Trim();
        if(double.TryParse(valueString, NumberStyles.Float, CultureInfo.InvariantCulture, out var r))
          rate = r;
      }

      if(line.Contains("time", StringComparison.InvariantCultureIgnoreCase))
      {
        var valueString = line.Split('=', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Trim();
        if(double.TryParse(valueString, NumberStyles.Float, CultureInfo.InvariantCulture, out var t))
          time = t;
      }
    }

    response = new SetTimeResponse(baseResponse.CommandEcho, baseResponse.ResponseLines, baseResponse.Raw, rate, time);
    return true;
  }
}
