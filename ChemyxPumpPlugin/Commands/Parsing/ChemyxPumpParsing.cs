using System.Text;
using ChemyxPumpPlugin.Commands.Responses;

namespace ChemyxPumpPlugin.Commands.Parsing;

internal static class ChemyxPumpParsing
{
  public static bool TryParse(byte[] buffer, string originalCommand, out ChemyxPumpResponse response, out ArraySegment<byte>? dataToRemove)
  {
    response = default!;
    dataToRemove = null;

    if(buffer is null || buffer.Length == 0)
      return false;

    var terminatorIdx = Array.IndexOf(buffer, (byte)'>');
    if(terminatorIdx < 0)
      return false;

    var length = terminatorIdx + 1;
    var raw = Encoding.ASCII.GetString(buffer, 0, length);
    var trimmed = raw.TrimEnd('>', '\r', '\n', '\0');
    var lines = trimmed.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

    var commandEcho = lines.FirstOrDefault() ?? string.Empty;
    if(commandEcho != originalCommand)
    {
      return false;
    }

    var payload = lines.Skip(1).ToArray();

    response = new ChemyxPumpResponse(commandEcho, payload, raw);
    dataToRemove = new ArraySegment<byte>(buffer, 0, length);
    return true;
  }
}
