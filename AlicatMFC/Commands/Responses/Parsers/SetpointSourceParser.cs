namespace AlicatMFC.Commands.Responses.Parsers;
internal class SetpointSourceParser : ResponseParser<SetpointSourceResponse>
{
  private readonly char _assumedId;

  public SetpointSourceParser(char assumedId)
  {
    _assumedId = assumedId;
  }
  
  protected override bool TryParseResponse(string line, out SetpointSourceResponse? response)
  {
    var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (tokens[0].First() != _assumedId)
    {
      response = null;
      return false;
    }
    
    var sourceToken = tokens.ElementAtOrDefault(1);
    if (string.IsNullOrEmpty(sourceToken))
    {
      response = null;
      return false;
    }

    response = new SetpointSourceResponse(_assumedId, SetpointSourceExtensions.FromStringSource(sourceToken));
    return true;
  }
}
