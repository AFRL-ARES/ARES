using Ares.Datamodel;
using AresScript.Generated;

namespace AresScript;

public record AresScriptLambda(
  string Name,
  IReadOnlyList<string> Parameters,
  AresLangParser.ExpressionContext Body,
  IReadOnlyDictionary<string, AresValue> Closure);
