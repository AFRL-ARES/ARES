using Ares.Toolkit.Serial.Commands;

namespace AlicatMFCRemastered.Commands.Responses.Parsers;

internal class GenericLineParser : AsciiResponseParser<GenericLineResponse>
{
  public GenericLineParser(char id)
  {
    Id = id;
  }

  public char Id { get; }

  protected override bool TryParseResponse(string line, out GenericLineResponse? response)
  {
    if(string.IsNullOrEmpty(line))
    {
      response = null;
      return false;
    }

    var id = line.First();
    if(!Enumerable.Range('A', 25).Select(c => (char)c).Contains(id))
    {
      response = null;
      return false;
    }

    if(id != Id)
    {
      response = null;
      return false;
    }

    response = new GenericLineResponse(id);
    return true;
  }
}
