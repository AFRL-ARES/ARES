using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Scripting;

namespace AresScript.ScriptAnalysis;

public static partial class AresScriptAnalysis
{
  public static AutocompleteCatalog BuildAutocompleteCatalog(AresScriptEnvironment env)
  {
    var systemFunctions = env.GetAllSystemFunctions();
    var systemVariables = env.GetAllSystemVariables();
    var namespaceMap = new Dictionary<string, NamespaceSymbol>(StringComparer.Ordinal);

    foreach(var func in systemFunctions)
    {
      var namespaceName = func.Namespace;
      if(!namespaceMap.TryGetValue(func.Namespace, out var ns))
      {
        ns = new NamespaceSymbol
        {
          NamespaceId = namespaceName,
          Identifier = namespaceName,
          DisplayName = namespaceName,
          Description = string.Empty,
          Kind = NamespaceKind.Device
        };
        namespaceMap[namespaceName] = ns;
      }

      ns.Functions.Add(new FunctionSymbol
      {
        Id = func.Id,
        Name = func.Name,
        Description = func.Description,
        InputSchema = func.InputSchema,
        OutputSchema = func.OutputSchema
      });
    }

    var response = new AutocompleteCatalog
    {
      CatalogVersion = string.Empty
    };
    response.Namespaces.AddRange(namespaceMap.Values);
    response.GlobalFunctions.AddRange(systemFunctions
      .Where(func => string.IsNullOrWhiteSpace(func.Namespace))
      .Select(func => new FunctionSymbol
      {
        Id = func.Id,
        Name = func.Name,
        Description = func.Description,
        InputSchema = func.InputSchema,
        OutputSchema = func.OutputSchema
      }));
    response.Globals.AddRange(systemVariables
      .Where(kv => kv.Value.RawValue?.KindCase != AresValue.KindOneofCase.FunctionValue)
      .Select(kv => new GlobalVariableSymbol
      {
        Name = kv.Key,
        Description = kv.Value.Description ?? string.Empty,
        Schema = kv.Value.ToAresValue().ToAresValueSchema(),
        Value = kv.Value.ToAresValue()
      }));
    return response;
  }

  public static CompletionItemKind MapNamespaceKindToCompletionKind(NamespaceKind kind)
  {
    return kind switch
    {
      NamespaceKind.Device => CompletionItemKind.Device,
      NamespaceKind.Planner => CompletionItemKind.Planner,
      NamespaceKind.Analyzer => CompletionItemKind.Analyzer,
      _ => CompletionItemKind.Unspecified
    };
  }
}
