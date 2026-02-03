using AresScript;

namespace Ares.Core.Scripting;

public class BaseEnvironmentBuilder(IEnumerable<ISystemFunctionProvider> systemFunctionProviders)
{
  private readonly IEnumerable<ISystemFunctionProvider> _systemFunctionProvider = systemFunctionProviders;

  public AresScriptEnvironment Build()
  {
    var env = new AresScriptEnvironment();
    var functions = _systemFunctionProvider.SelectMany(sfp => sfp.GetFunctions()).ToArray();
    if(functions.Length == 0)
    {
      return env;
    }

    // let's stick with global scope for now
    env.AssignSystemFunctions(StandardLibrary.Functions);
    env.AssignSystemFunctions(functions);
    env.AssignSystemVariables(BuildSystemValues(env));

    return env;
  }

  private static IEnumerable<KeyValuePair<string, AresSystemValue>> BuildSystemValues(AresScriptEnvironment environment)
  {
    var rootVariables = new Dictionary<string, AresSystemValue>(StringComparer.Ordinal);
    var functions = environment.GetAllSystemFunctions();

    foreach(var func in functions)
    {
      var fieldName = string.IsNullOrWhiteSpace(func.Name) ? func.Id : func.Name;
      if(string.IsNullOrWhiteSpace(func.Namespace))
      {
        if(!string.IsNullOrWhiteSpace(fieldName) && !rootVariables.ContainsKey(fieldName))
        {
          rootVariables[fieldName] = AresSystemValue.Function(func);
        }
        continue;
      }

      if(string.IsNullOrWhiteSpace(fieldName))
      {
        continue;
      }

      var pathSegments = func.Namespace
        .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
      if(pathSegments.Length == 0)
      {
        continue;
      }

      AddFunctionToPath(rootVariables, pathSegments, fieldName, func);
    }

    return rootVariables.Select(kv => new KeyValuePair<string, AresSystemValue>(kv.Key, kv.Value));
  }

  private static void AddFunctionToPath(
    IDictionary<string, AresSystemValue> root,
    string[] pathSegments,
    string fieldName,
    AresSystemFunction func)
  {
    if(pathSegments.Length == 0)
    {
      var funcValue = AresSystemValue.Function(func);
      root[fieldName] = funcValue;
      return;
    }

    var segment = pathSegments.Last();

    var valueExists = root.TryGetValue(segment, out var value);

    if(!valueExists || value is null)
    {
      var newRoot = AresSystemValue.Struct();
      root[segment] = newRoot;
      AddFunctionToPath(newRoot.StructFields!, pathSegments[..^1], fieldName, func);
      return;
    }

    if(value.Kind != AresSystemValue.AresSystemValueKind.Struct || value.StructFields is null)
    {
      throw new InvalidOperationException($"Value {segment} already exists and is not a struct, but rather a {value.Kind}");
    }

    AddFunctionToPath(value.StructFields, pathSegments[..^1], fieldName, func);
  }
}
