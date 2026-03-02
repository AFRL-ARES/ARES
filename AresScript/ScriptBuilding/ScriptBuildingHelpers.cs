using Ares.Datamodel;
using AresScript.Generated;
using AresScript.Symbols;
using System.Text;

namespace AresScript.ScriptBuilding;

internal static class ScriptBuildingHelpers
{
  public static string ToFunctionSignature(this AresScriptParameter parameter)
  {
    return $"{parameter.Name}: {parameter.Type}";
  }

  public static AresScriptParameter StringToScriptParam(string paramString)
  {
    var splitParam = paramString.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    var id = splitParam.FirstOrDefault();
    if(string.IsNullOrEmpty(id))
    {
      throw new InvalidOperationException("Parameter id was blank for some reason.");
    }

    var typeHintStr = splitParam.ElementAtOrDefault(1);
    if(string.IsNullOrEmpty(typeHintStr) || !Enum.TryParse<AresDataType>(typeHintStr, out var typeHint))
    {
      return new AresScriptParameter(id, AresDataType.Any);
    }

    return new AresScriptParameter(id, typeHint);
  }

  public static string ToParameterSignature(this AresLangParser.ParameterContext parameterContext)
  {
    var id = parameterContext.ID().GetText();
    var type = parameterContext.typeHint()?.ID().FirstOrDefault()?.GetText() ?? "";
    var builder = new StringBuilder(id);
    if(!string.IsNullOrEmpty(type))
    {
      builder.Append(": ").Append(type);
    }

    return builder.ToString();
  }
}
