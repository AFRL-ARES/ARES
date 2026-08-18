using System.Text.RegularExpressions;

namespace AresScript.Interpreters;

internal static class InterpreterHelpers
{
  public static string Unquote(string raw)
  {
    var unquoted = raw[1..^1];
    var value = Regex.Unescape(unquoted);

    return value;
  }
}
