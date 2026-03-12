namespace VerdiV6Laser.Responses.Parsers;

internal class LaserPowerParser : ResponseParser<LaserPowerResponse>
{
  protected override bool TryParseResponse(string line, out LaserPowerResponse? response)
  {
    if (string.IsNullOrEmpty(line))
    {
      response = null;
      return false;
    }

    // Example line might be "0.00" or similar depending on the protocol
    // Original code used line.Substring(getterLength, 4) which is highly specific
    // and prone to errors if the format changes slightly.
    // Let's try to parse the entire line as a double.
    
    if (double.TryParse(line.Trim(), out double result))
    {
        response = new LaserPowerResponse(result);
        return true;
    }

    response = null;
    return false;
  }
}
