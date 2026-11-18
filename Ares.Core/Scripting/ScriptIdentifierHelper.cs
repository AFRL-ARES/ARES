using System.Text;

namespace Ares.Core.Scripting;

public static class ScriptIdentifierHelper
{
  public static string SanitizeIdentifier(string value)
  {
    if(string.IsNullOrWhiteSpace(value))
      return "_";

    var builder = new StringBuilder(value.Length);
    foreach(var character in value)
      builder.Append(IsAsciiIdentifierCharacter(character) ? character : '_');

    if(builder.Length == 0 || !IsAsciiIdentifierStart(builder[0]))
      builder.Insert(0, '_');

    return builder.ToString();
  }

  private static bool IsAsciiIdentifierCharacter(char value)
    => IsAsciiIdentifierStart(value) || value is >= '0' and <= '9';

  private static bool IsAsciiIdentifierStart(char value)
    => value is >= 'a' and <= 'z'
      or (>= 'A' and <= 'Z')
      or '_';
}
