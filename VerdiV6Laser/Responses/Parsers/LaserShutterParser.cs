using VerdiV6Laser.Responses;

namespace VerdiV6Laser.Responses.Parsers;

internal class LaserShutterParser : ResponseParser<LaserShutterResponse>
{
  protected override bool TryParseResponse(string line, out LaserShutterResponse? response)
  {
    if (string.IsNullOrEmpty(line))
    {
      response = null;
      return false;
    }

    // Assumes response contains "1" for open and "0" for closed
    response = new LaserShutterResponse(line.Contains('1'));
    return true;
  }
}
