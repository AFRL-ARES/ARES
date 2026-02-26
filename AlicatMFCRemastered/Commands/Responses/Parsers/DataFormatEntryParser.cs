using System.Text.RegularExpressions;
using AlicatMFCRemastered.Commands.Responses.Streamed;
using Ares.Toolkit.Serial.Commands;
using Parsers.AlicatMFCRemastered;

namespace AlicatMFCRemastered.Commands.Responses.Parsers;

internal class DataFormatEntryParser : AsciiResponseParser<DataFrameFormatEntry>
{

  private static readonly Regex _identifierExpression = new(@"D\d\d");
  private static readonly Regex _unitIdExpression = new(@"\b[A-Z]\b");
  private static readonly string _headerIdentifer = "D00";
  private static HeaderTokenType[]? _headerTypes;
  private readonly char _assumedId;
  private readonly int? _entryNumber;

  public DataFormatEntryParser(char assumedId, int? entryNumber)
  {
    _assumedId = assumedId;
    _entryNumber = entryNumber;
  }

  /// <summary>
  /// </summary>
  /// <param name="msg">The unit abbreviation (ex.: PSI, C, etc.)</param>
  /// <param name="unitType"></param>
  /// <returns></returns>
  public static Enum? GetUnitFromAbbreviation(string? msg, Type? unitType)
  {
    if(unitType is null || string.IsNullOrEmpty(msg))
      return null;

    // The MFC likes to report an absolute pressure unit for its pressure, but I'm not sure we have a UnitsNet equivalent,
    // so for now we'll just replace it with a more typical PSI unit

    _ = MfcUnitParser.Parser.TryParse(msg, unitType, out var unitEnum);
    return unitEnum;
  }

  public static Enum? GetUnitFromEnumString(string? msg, Type? unitType)
  {
    if(unitType is null || string.IsNullOrEmpty(msg))
      return null;

    var info = Enum.TryParse(unitType, msg, out var unitEnum);
    return (Enum?)unitEnum;
  }

  private static DataFormatField GetDataField(string msg)
    => msg.ToDataFormatField();

  private static HeaderTokenType[] GetHeaderTypes(string header)
  {
    var headerTokens = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var headerTypes = new HeaderTokenType[headerTokens.Length];
    for(var i = 0; i < headerTokens.Length; i++)
    {
      var token = headerTokens[i];
      var unitIdMatch = _unitIdExpression.Match(token);
      if(unitIdMatch.Success)
        headerTypes[i] = HeaderTokenType.Id;
      else if(token.StartsWith("name", StringComparison.InvariantCultureIgnoreCase))
        headerTypes[i] = HeaderTokenType.Name;
      else if(token.StartsWith("type", StringComparison.InvariantCultureIgnoreCase))
        headerTypes[i] = HeaderTokenType.Type;
      else if(token.StartsWith("MinVal", StringComparison.InvariantCultureIgnoreCase))
        headerTypes[i] = HeaderTokenType.MinVal;
      else if(token.StartsWith("MaxVal", StringComparison.InvariantCultureIgnoreCase))
        headerTypes[i] = HeaderTokenType.MaxVal;
      else if(token.StartsWith("Unit", StringComparison.InvariantCultureIgnoreCase))
        headerTypes[i] = HeaderTokenType.Units;
      else if(token.EndsWith("00", StringComparison.InvariantCultureIgnoreCase))
        headerTypes[i] = HeaderTokenType.LineNumber;
      else if(token.StartsWith("Notes", StringComparison.InvariantCultureIgnoreCase))
        headerTypes[i] = HeaderTokenType.Notes;
      else if(token.StartsWith("Width", StringComparison.InvariantCultureIgnoreCase))
        headerTypes[i] = HeaderTokenType.Width;
      else if(token.StartsWith("id", StringComparison.InvariantCultureIgnoreCase))
        headerTypes[i] = HeaderTokenType.DataFrameId;
    }

    return headerTypes;
  }

  protected override bool TryParseResponse(string line, out DataFrameFormatEntry? response)
  {
    var isEndMarker = line.EndsWith('?') || line.StartsWith('?');
    if(isEndMarker)
    {
      response = new DataFrameFormatEntry(_assumedId, DataFrameFormatEntryType.EndMarker);
      return true;
    }

    if(line[0] != _assumedId)
    {
      response = null;
      return false;
    }

    var lineCpy = AlphaNumericize(line);
    lineCpy = DespaceNames(lineCpy);

    var identifierMatch = _identifierExpression.Match(lineCpy);
    if(!identifierMatch.Success)
    {
      response = null;
      return false;
    }

    var identifier = identifierMatch.Value;
    var isHeader = identifier.Equals(_headerIdentifer);
    if(isHeader)
    {
      _headerTypes = GetHeaderTypes(lineCpy);
      response = new DataFrameFormatEntry(_assumedId, DataFrameFormatEntryType.Header);// We don't really care about the header, but we want to say the line was parsed successfully so it is removed from zeh buffer.
      return true;
    }

    if(_headerTypes is null)
      throw new InvalidOperationException("Header was undefined when trying to parse a data format entry.");


    var tokens = lineCpy.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var dataFieldStr = tokens[Array.IndexOf(_headerTypes, HeaderTokenType.Name)];
    var dataField = GetDataField(dataFieldStr);
    var idStr = tokens[Array.IndexOf(_headerTypes, HeaderTokenType.Id)];
    var lineNumberStr = tokens[Array.IndexOf(_headerTypes, HeaderTokenType.LineNumber)];
    var lineNumber = int.Parse(lineNumberStr[1..]);
    if(_entryNumber.HasValue && lineNumber != _entryNumber.Value)
    {
      response = null;
      return false;
    }
    var type = tokens[Array.IndexOf(_headerTypes, HeaderTokenType.Type)];
    var minVal = tokens.ElementAtOrDefault(Array.IndexOf(_headerTypes, HeaderTokenType.MinVal));
    var maxVal = tokens.ElementAtOrDefault(Array.IndexOf(_headerTypes, HeaderTokenType.MaxVal));
    var unitStr = tokens.ElementAtOrDefault(Array.IndexOf(_headerTypes, HeaderTokenType.Units));
    var widthStr = tokens.ElementAtOrDefault(Array.IndexOf(_headerTypes, HeaderTokenType.Width));
    var notesArr = Array.IndexOf(_headerTypes, HeaderTokenType.Notes) != -1 ? tokens[Array.IndexOf(_headerTypes, HeaderTokenType.Notes)..] : Array.Empty<string>();
    var unit = GetUnitFromAbbreviation(unitStr ?? notesArr.LastOrDefault(), dataField.ToUnitType());
    var formatEntry = new DataFrameFormatEntry(idStr.First(), lineNumber, dataField, type, minVal, maxVal, widthStr, string.Join(' ', notesArr), unit);
    response = formatEntry;
    return true;
  }

  private string AlphaNumericize(string line)
    => new string(line.Where(chr => chr == '_' || (chr >= 'a' && chr <= 'z') || (chr >= 'A' && chr <= 'Z') || (chr >= '0' && chr <= '9') || chr == '-' || chr == '+' || chr == '\t' || chr == '\r' || chr == ' ' || chr == '.').ToArray());
  private string DespaceNames(string line)
  {
    var despaced = line.Replace("Unit ID", "UnitID", StringComparison.InvariantCultureIgnoreCase)
    .Replace("Abs Press", "AbsPress", StringComparison.InvariantCultureIgnoreCase)
    .Replace("Flow Temp", "FlowTemp", StringComparison.InvariantCultureIgnoreCase)
    .Replace("Volu Flow", "VoluFlow", StringComparison.InvariantCultureIgnoreCase)
    .Replace("Mass Flow", "MassFlow", StringComparison.InvariantCultureIgnoreCase)
    .Replace("MassFlow Setpt", "MassFlowSetpt", StringComparison.InvariantCultureIgnoreCase)
    .Replace("s decimal", "sdecimal", StringComparison.InvariantCultureIgnoreCase);
    return despaced;
  }

  private enum HeaderTokenType
  {
    Undefined,
    Width,
    Notes,
    Id,
    DataFrameId,
    LineNumber,
    Name,
    Type,
    MinVal,
    MaxVal,
    Units
  }
}
