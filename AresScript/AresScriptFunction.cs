using Ares.Datamodel;
using AresScript.Generated;

namespace AresScript;

public record AresScriptParameter(string Name, AresDataType Type = AresDataType.Any);

public record AresScriptFunction(
  string Name,
  IReadOnlyList<AresScriptParameter> Parameters,
  AresLangParser.FuncBlockContext Body,
  AresDataType ReturnType = AresDataType.Any)
{
  public IReadOnlyList<string> ParameterNames { get; } = Parameters.Select(parameter => parameter.Name).ToArray();
}
