namespace VerdiV6Laser.Commands.Responses.Parsers
{
  internal class LaserShutterParser : ResponseParser<LaserShutterResponse>
  {
    public LaserShutterParser()
    {

    }

    protected override bool TryParseResponse(string line, out LaserShutterResponse? response)
    {
      if(string.IsNullOrEmpty(line))
      {
        response = null;
        return false;
      }

      response = new LaserShutterResponse();
      response.Shutter = line.Contains("1");
      return true;
    }
  }
}
