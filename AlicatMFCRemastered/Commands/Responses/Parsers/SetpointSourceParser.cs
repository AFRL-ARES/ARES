using AlicatMFCRemastered.Commands.Extensions;
using Ares.Toolkit.Serial.Commands;

namespace AlicatMFCRemastered.Commands.Responses.Parsers;
internal class SetpointSourceParser : AsciiResponseParser<SetpointSourceResponse>
{
  private readonly char _assumedId;

  public SetpointSourceParser(char assumedId)
  {
    _assumedId = assumedId;
  }

  protected override bool TryParseResponse(string line, out SetpointSourceResponse? response)
  {
    var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if(tokens[0].First() != _assumedId)
    {
      response = null;
      return false;
    }

    var sourceToken = tokens.ElementAtOrDefault(1);
    if(string.IsNullOrEmpty(sourceToken))
    {
      response = null;
      return false;
    }
    sourceToken = sourceToken.ToUpper();
    if(sourceToken != "U" && sourceToken != "A" && sourceToken != "S")
    {
      response = null;
      return false;
    }

    response = new SetpointSourceResponse(_assumedId, SetpointSourceExtensions.FromStringSource(sourceToken));
    return true;
  }
}
