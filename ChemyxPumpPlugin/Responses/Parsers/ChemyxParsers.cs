using Ares.Toolkit.Serial.Commands;
using ChemyxPumpPlugin.Enums;
using ChemyxPumpPlugin.Responses;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ChemyxPumpPlugin.Responses.Parsers;

public abstract class ChemyxResponseParserBase<T>(string originalCommand) : SerialResponseParser<T> where T : SerialResponse
{
  protected readonly string OriginalCommand = originalCommand;

  public override bool TryParseResponse(byte[] buffer, out T? response, out ArraySegment<byte>? dataToRemove)
  {
    response = null;
    dataToRemove = null;

    if(buffer == null || buffer.Length == 0) return false;

    var terminatorIdx = Array.IndexOf(buffer, (byte)'>');
    if(terminatorIdx < 0) return false;

    var utfProxy = Encoding.UTF8.GetString(buffer);
    var useLast = utfProxy.EndsWith('>');
    var commandResponses = utfProxy.Split('>', StringSplitOptions.None);
    if(!useLast) commandResponses = commandResponses.SkipLast(1).ToArray();

    var startIdx = 0;
    for(var i = 0; i < commandResponses.Length; i++)
    {
      var cmdResponse = commandResponses[i];
      var lines = cmdResponse.Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries);
      var commandEcho = lines.FirstOrDefault() ?? string.Empty;
      
      if(commandEcho.Trim() != OriginalCommand.Trim())
      {
        startIdx += cmdResponse.Length + 1;
        continue;
      }

      var payload = lines.Skip(1).ToArray();
      response = CreateResponse(commandEcho, payload, cmdResponse);
      dataToRemove = new ArraySegment<byte>(buffer, startIdx, cmdResponse.Length + 1);
      return true;
    }

    return false;
  }

  protected abstract T CreateResponse(string echo, string[] lines, string raw);
}

public class ChemyxResponseParser(string originalCommand) : ChemyxResponseParserBase<ChemyxResponse>(originalCommand)
{
  protected override ChemyxResponse CreateResponse(string echo, string[] lines, string raw) => new(echo, lines, raw);
}

public class NumericResponseParser(string originalCommand) : ChemyxResponseParserBase<NumericResponse>(originalCommand)
{
  protected override NumericResponse CreateResponse(string echo, string[] lines, string raw)
  {
    double? parsed = null;
    var line = lines.FirstOrDefault();
    if(!string.IsNullOrWhiteSpace(line))
    {
      var lastPart = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
      if(double.TryParse(lastPart, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        parsed = value;
    }
    return new NumericResponse(echo, lines, raw, parsed);
  }
}

public class PumpStatusResponseParser(string originalCommand) : ChemyxResponseParserBase<PumpStatusResponse>(originalCommand)
{
  protected override PumpStatusResponse CreateResponse(string echo, string[] lines, string raw)
  {
    PumpStatus? status = null;
    var line = lines.FirstOrDefault();
    if(!string.IsNullOrWhiteSpace(line) && int.TryParse(line.Trim(), out var s))
      status = (PumpStatus)s;
    return new PumpStatusResponse(echo, lines, raw, status);
  }
}

public class LimitParameterResponseParser(string originalCommand) : ChemyxResponseParserBase<LimitParameterResponse>(originalCommand)
{
  protected override LimitParameterResponse CreateResponse(string echo, string[] lines, string raw)
  {
    var line = lines.FirstOrDefault();
    var parts = line?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
    double?[] values = new double?[3];
    for(var i = 0; i < Math.Min(parts.Length, 3); i++)
    {
      if(double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
        values[i] = v;
    }
    return new LimitParameterResponse(echo, lines, raw, values[0] ?? -1.0, values[1] ?? -1.0, values[2] ?? -1.0, values[3] ?? -1.0);
  }
}

public class SetTimeResponseParser(string originalCommand) : ChemyxResponseParserBase<SetTimeResponse>(originalCommand)
{
  protected override SetTimeResponse CreateResponse(string echo, string[] lines, string raw)
  {
    double? rate = null, time = null;
    foreach(var line in lines)
    {
      if(line.Contains("rate", StringComparison.OrdinalIgnoreCase))
      {
        if(double.TryParse(line.Split('=').LastOrDefault()?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var r)) rate = r;
      }
      if(line.Contains("time", StringComparison.OrdinalIgnoreCase))
      {
        if(double.TryParse(line.Split('=').LastOrDefault()?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var t)) time = t;
      }
    }
    return new SetTimeResponse(echo, lines, raw, rate, time);
  }
}

public partial class ViewParametersResponseParser(string originalCommand) : ChemyxResponseParserBase<ViewParametersResponse>(originalCommand)
{
  protected override ViewParametersResponse CreateResponse(string echo, string[] lines, string raw)
  {
    var cleanedLines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

    if(cleanedLines.Length > 0 && cleanedLines[0].StartsWith("pump", StringComparison.OrdinalIgnoreCase))
    {
      var split = SplitByPump(lines).ToArray();
      var p1 = ParsePump(split[0]);
      var p2 = split.Length > 1 ? ParsePump(split[1]) : null;
      return new ViewParametersResponse(echo, cleanedLines, raw, p1, p2);
    }

    return new ViewParametersResponse(echo, cleanedLines, raw, ParsePump(cleanedLines), null);
  }

  private SinglePumpParameters ParsePump(string[] lines)
  {
    var unit = PumpUnits.MillilitersPerMinute;
    double dia = 0, rate = 0, vol = 0, delay = 0;
    foreach(var line in lines)
    {
      var l = line.ToLower();
      var match = PumpParamRegex().Match(l);
      if(!match.Success) 
        continue;
      
      var val = match.Groups[1].Value;
      if(l.StartsWith("unit")) 
        unit = (PumpUnits)int.Parse(val);
      
      else if(l.StartsWith("dia")) 
        dia = double.Parse(val);

      else if(l.StartsWith("rate")) 
        rate = double.Parse(val);

      else if(l.StartsWith("vol")) 
        vol = double.Parse(val);

      else if(l.StartsWith("delay")) 
        delay = double.Parse(val);
    }
    return new SinglePumpParameters(dia, vol, rate, delay, unit);
  }

  private IEnumerable<string[]> SplitByPump(string[] lines)
  {
    List<string>? current = null;
    foreach(var line in lines)
    {
      if(line.StartsWith("pump", StringComparison.OrdinalIgnoreCase))
      {
        if(current != null) yield return current.ToArray();
        current = [];
      }
      else current?.Add(line);
    }
    if(current != null) yield return current.ToArray();
  }

  [GeneratedRegex("\\w+\\s=\\s(\\S+)")]
  private static partial Regex PumpParamRegex();
}
