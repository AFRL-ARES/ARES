using System.Text.RegularExpressions;
using AlicatMFCRemastered.Commands.Responses.Streamed;
using Ares.Device.Serial.Commands;

namespace AlicatMFCRemastered.Commands.Responses.Parsers;

internal class GasInfoListParser : AsciiResponseParser<GasInfoEntryList>
{
  // private static Regex _identifierExpression = new(@"G\d\d\s+");
  // private static Regex _unitIdExpression = new(@"[A-Z]\s+");
  // private static Regex _gasNameExpression = new(@"\w+");
  private static readonly Regex _gasInfoEntryRegex = new(@"[A-Z]\s+\d+\s+\w+");
  private char _assumedId;

  public GasInfoListParser(char assumedId)
  {
    _assumedId = assumedId;
  }
  protected override bool TryParseResponse(string line, out GasInfoEntryList? gasInfoEntry)
  {
    if(line.EndsWith('?') || line.StartsWith('?'))
    {
      gasInfoEntry = null;
      return true;
    }

    var lineCpy = line.Replace("\b", "");
    var gasEntries = lineCpy.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    var gasList = new List<GasInfoEntry>();
    foreach(var entry in gasEntries)
    {
      var entryMatch = _gasInfoEntryRegex.Match(entry);
      if(!entryMatch.Success)
      {
        gasInfoEntry = null;
        return false;
      }

      var tokens = entry.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      if(tokens[0][0] != _assumedId)
      {
        gasInfoEntry = null;
        return false;
      }
      var indexStr = tokens[1];
      var index = uint.Parse(indexStr);
      var gasName = tokens[2];
      gasList.Add(new GasInfoEntry(tokens[0][0], gasName, index));
    }

    gasInfoEntry = new GasInfoEntryList(_assumedId, gasList);
    return true;
  }
}
