using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;

namespace Ares.Core.Execution.Executors;

internal static class CommandVariableResolver
{
  public static Dictionary<string, AresValue> CreateVariableScope(IEnumerable<CommandExecutionSummary> commandSummaries)
  {
    var scope = new Dictionary<string, AresValue>();
    foreach(var commandSummary in commandSummaries)
    {
      if(!commandSummary.HasVarName || commandSummary.Result?.Success != true || commandSummary.Result.Result is null)
        continue;

      foreach(var variable in Flatten(commandSummary.VarName, commandSummary.Result.Result))
        scope[variable.Key] = variable.Value;
    }

    return scope;
  }

  public static string? ResolveParameters(IEnumerable<Parameter> parameters, IReadOnlyDictionary<string, AresValue> variableScope)
  {
    foreach(var parameter in parameters.Where(parameter => parameter.GetParameterSource() == ParameterSource.Variable))
    {
      var variableArgument = parameter.GetVariableArgument();
      if(string.IsNullOrWhiteSpace(variableArgument))
        return $"Parameter {parameter.Metadata.Name} is configured to use a command output variable, but no variable was selected.";

      if(!variableScope.TryGetValue(variableArgument, out var value))
        return $"Parameter {parameter.Metadata.Name} references unavailable command output variable {variableArgument}.";

      var expectedType = parameter.Metadata.Schema?.Type ?? AresDataType.Any;
      var actualType = value.ToAresValueSchema().Type;
      if(!IsCompatible(expectedType, actualType))
        return $"Parameter {parameter.Metadata.Name} expects {expectedType}, but variable {variableArgument} is {actualType}.";

      parameter.SetResolvedValue(value.Clone());
    }

    return null;
  }

  private static IEnumerable<KeyValuePair<string, AresValue>> Flatten(string path, AresValue value)
  {
    yield return KeyValuePair.Create(path, value);

    if(value.KindCase != AresValue.KindOneofCase.StructValue)
      yield break;

    foreach(var field in value.StructValue.Fields)
    {
      foreach(var nestedValue in Flatten($"{path}.{field.Key}", field.Value))
        yield return nestedValue;
    }
  }
  // TODO: This one might need a bit of extra logic based on schema instead of plain datatype
  // unless we just use non-struct variables.
  private static bool IsCompatible(AresDataType expectedType, AresDataType actualType)
  {
    if(expectedType == AresDataType.Any || actualType == AresDataType.Any || expectedType == actualType)
      return true;
    
    if(expectedType == AresDataType.Number && actualType == AresDataType.Float || actualType == AresDataType.Int)
      return true;

    return false;
  }
    
}
