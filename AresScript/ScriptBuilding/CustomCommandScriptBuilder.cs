using Ares.Datamodel;
using AresScript.Symbols;
using System.Text;
using System.Text.RegularExpressions;

namespace AresScript.ScriptBuilding;

public static class CustomCommandScriptBuilder
{
  private const string FunctionPrefix = "custom_command_";
  private const string EmptyBodyFallback = "return";
  private static readonly Regex InvalidIdentifierCharacterRegex = new("[^a-zA-Z0-9_]+", RegexOptions.Compiled);
  private static readonly Regex RepeatedUnderscoreRegex = new("_+", RegexOptions.Compiled);

  public static string BuildFunctionName(string commandName)
  {
    var safeName = string.IsNullOrWhiteSpace(commandName) ? "command" : commandName.Trim();
    safeName = InvalidIdentifierCharacterRegex.Replace(safeName, "_");
    safeName = RepeatedUnderscoreRegex.Replace(safeName, "_").Trim('_');

    if(string.IsNullOrWhiteSpace(safeName))
    {
      safeName = "command";
    }

    return $"{FunctionPrefix}{safeName}";
  }

  public static string BuildFunctionSignature(
    string commandName,
    IEnumerable<AresScriptParameter> parameters,
    AresValueSchema? returnSchema = null)
  {
    ArgumentNullException.ThrowIfNull(parameters);
    return ScriptBuildingHelpers.BuildFunctionSignature(BuildFunctionName(commandName), parameters, returnSchema);
  }

  public static string BuildWrappedScript(
    string commandName,
    IEnumerable<AresScriptParameter> parameters,
    AresValueSchema? returnSchema,
    string? scriptBody)
  {
    var signature = BuildFunctionSignature(commandName, parameters, returnSchema);
    var normalizedBody = NormalizeBody(scriptBody);
    var output = new StringBuilder();
    output.Append(signature).AppendLine(":");

    foreach(var line in normalizedBody.Split('\n'))
    {
      output.Append("  ").AppendLine(line);
    }

    if(string.IsNullOrWhiteSpace(normalizedBody))
    {
      output.Append("  ").Append(EmptyBodyFallback);
    }

    return output.ToString();
  }

  private static string NormalizeBody(string? scriptBody)
  {
    return scriptBody?
      .Replace("\r\n", "\n", StringComparison.Ordinal)
      .Replace('\r', '\n')
      ?? string.Empty;
  }
}
