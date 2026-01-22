using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using AresScript;
using System.Collections.Generic;
using System.Linq;

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
    env.AssignSystemVariables(BuildNamespaceVariables(functions));

    return env;
  }

  private static IEnumerable<KeyValuePair<string, AresValue>> BuildNamespaceVariables(IEnumerable<AresSystemFunction> functions)
  {
    var namespaces = new Dictionary<string, AresStruct>(StringComparer.Ordinal);

    foreach(var func in functions)
    {
      if(string.IsNullOrWhiteSpace(func.Namespace))
      {
        continue;
      }

      if(!namespaces.TryGetValue(func.Namespace, out var structValue))
      {
        structValue = new AresStruct();
        namespaces[func.Namespace] = structValue;
      }

      var fieldName = string.IsNullOrWhiteSpace(func.Name) ? func.Id : func.Name;
      if(string.IsNullOrWhiteSpace(fieldName))
      {
        continue;
      }

      if(!structValue.Fields.ContainsKey(fieldName))
      {
        structValue.Fields[fieldName] = AresValueHelper.CreateFunction(func.Id);
      }
    }

    return namespaces.Select(kv => new KeyValuePair<string, AresValue>(kv.Key, AresValueHelper.CreateStruct(kv.Value)));
  }
}
