using Ares.Datamodel;
using AresScript.Generated;
using AresScript.Symbols;
using System.Text;

namespace AresScript.ScriptBuilding;

internal static class ScriptBuildingHelpers
{
  public static string ToFunctionSignature(this AresScriptParameter parameter)
  {
    return $"{parameter.Name}: {AresScriptSchemaFormatter.ToTypeHint(parameter.Schema)}";
  }

  public static string BuildFunctionSignature(string functionName, IEnumerable<AresScriptParameter> parameters, SchemaEntry? returnSchema = null)
  {
    var normalizedParameters = parameters.ToArray();
    var signature = normalizedParameters.Length == 0
      ? $"def {functionName}()"
      : $"def {functionName}({string.Join(", ", normalizedParameters.Select(p => p.ToFunctionSignature()))})";

    if(returnSchema is not null)
    {
      signature += $" -> {AresScriptSchemaFormatter.ToTypeHint(returnSchema)}";
    }

    return signature;
  }

  public static string ToParameterSignature(this AresLangParser.ParameterContext parameterContext)
  {
    var id = parameterContext.ID().GetText();
    var type = parameterContext.typeHint()?.GetText() ?? "";
    var builder = new StringBuilder(id);
    if(!string.IsNullOrEmpty(type))
    {
      builder.Append(": ").Append(type);
    }

    return builder.ToString();
  }

  public static AresScriptParameter ToScriptParameter(this AresLangParser.ParameterContext parameterContext)
  {
    var parameterName = parameterContext.ID().GetText();
    var parameterSchema = AresScriptTypeHints.SchemaFromTypeHint(parameterContext.typeHint());
    return new AresScriptParameter(parameterName, parameterSchema);
  }
}
