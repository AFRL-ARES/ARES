using System.Text.RegularExpressions;
using AlicatMFCRemastered.Commands.Responses.Streamed;
using Ares.Toolkit.Serial.Commands;

namespace AlicatMFCRemastered.Commands.Responses.Parsers;

internal class GasInfoEntryParser : AsciiResponseParser<GasInfoEntry>
{
  private static readonly Regex _gasInfoEntryRegex = new(@"[A-Z]\s+G\d+\s+\w+");
  private char _assumedId;
  private int? _gasIdx;

  public GasInfoEntryParser(char assumedId, int? gasIdx = null)
  {
    _assumedId = assumedId;
    _gasIdx = gasIdx;
  }
  protected override bool TryParseResponse(string line, out GasInfoEntry? gasInfoEntry)
  {
    if(line.EndsWith('?') || line.StartsWith('?'))
    {
      gasInfoEntry = new GasInfoEntry(_assumedId);
      return true;
    }

    var lineCpy = line.Replace("\b", "");
    var entryMatch = _gasInfoEntryRegex.Match(lineCpy);
    if(!entryMatch.Success)
    {
      gasInfoEntry = null;
      return false;
    }

    var tokens = lineCpy.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if(tokens[0][0] != _assumedId)
    {
      gasInfoEntry = null;
      return false;
    }
    var indexStr = tokens[1][1..];
    var index = uint.Parse(indexStr);
    if(_gasIdx.HasValue && index != _gasIdx)
    {
      gasInfoEntry = null;
      return false;
    }
    var gasName = tokens[2];
    gasInfoEntry = new GasInfoEntry(tokens[0][0], gasName, index);

    return true;
  }
}
