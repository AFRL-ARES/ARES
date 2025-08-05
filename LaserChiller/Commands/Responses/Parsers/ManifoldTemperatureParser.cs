namespace LaserChiller.Commands.Responses.Parsers;

public class ManifoldTemperatureParser : ResponseParser<GetManifoldTemperatureResponse>
{
  public ManifoldTemperatureParser()
  {
  }

  protected override bool TryParseResponse(string line, out GetManifoldTemperatureResponse? response)
  {
    if(string.IsNullOrWhiteSpace(line))
    {
      response = null;
      return false;
    }

    var formattedResponse = line.Substring("#I0".Length, 5);

    var signCharacter = formattedResponse[0];
    var formattedTempData = formattedResponse.Substring(1).Insert("##".Length, ".");
    var temp = Convert.ToDouble(formattedTempData);

    if(signCharacter == '-')
      temp *= -1;

    response = new GetManifoldTemperatureResponse(temp);
    return true;
  }
}
