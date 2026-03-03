using Ares.Datamodel;
using AresScript.Generated;
using AresScript.Symbols;
using System.Text;

namespace AresScript.ScriptBuilding;

internal static class ScriptBuildingHelpers
{
  public static string ToFunctionSignature(this AresScriptParameter parameter)
  {
    return $"{parameter.Name}: {AresScriptTypeHints.ToTypeHintString(parameter.Schema)}";
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
