
namespace VerdiV6Laser.Commands.Responses.Parsers
{
  internal class LaserPowerParser : ResponseParser<LaserPowerResponse>
  {
    public LaserPowerParser()
    {

    }

    protected override bool TryParseResponse(string line, out LaserPowerResponse? response)
    {
      if(string.IsNullOrEmpty(line))
      {
        response = null;
        return false;
      }

      var getterLength = "?SP".Length;
      var responseParsed = double.TryParse(line.Substring(getterLength, 4), out double result);

      if(line.Length <= getterLength + 4 || !responseParsed)
      {
        response = null;
        return false;
      }

      response = new LaserPowerResponse(result);
      return true;
    }
  }
}
