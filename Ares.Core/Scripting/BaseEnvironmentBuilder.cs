using AresScript;
using AresScript.Environment;
using AresScript.Symbols;

namespace Ares.Core.Scripting;

public class BaseEnvironmentBuilder(IEnumerable<ISymbolProvider> symbolProviders)
{
  private readonly IEnumerable<ISymbolProvider> _symbolProviders = symbolProviders;

  public async Task<AresScriptEnvironment> Build()
  {
    var env = new AresScriptEnvironment();
    var providedSymbolTasks = await Task.WhenAll(_symbolProviders.Select(provider => provider.GetSymbols()));
    var providedSymbols = providedSymbolTasks.SelectMany(x => x).ToArray();
    var allSystemFunctions = StandardLibrary.Functions
      .Concat(providedSymbols.OfType<AresSystemFunctionSymbol>())
      .ToArray();

    env.AssignSystemFunctions(allSystemFunctions);
    env.AssignExtensionFunctions(StandardLibrary.ExtensionFunctions);
    env.AssignSystemVariables(BuildSystemValues(allSystemFunctions, providedSymbols.OfType<IValueSymbol>()));

    return env;
  }

  private static IEnumerable<KeyValuePair<string, AresSystemValue>> BuildSystemValues(
    IEnumerable<AresSystemFunctionSymbol> systemFunctions,
    IEnumerable<IValueSymbol> providedValueSymbols)
  {
    var rootVariables = new Dictionary<string, AresSystemValue>(StringComparer.Ordinal);

    foreach(var func in systemFunctions)
    {
      var fieldName = string.IsNullOrWhiteSpace(func.Name) ? func.Id : func.Name;
      var parentPath = !string.IsNullOrWhiteSpace(func.ParentName)
        ? func.ParentName
        : func.Namespace;

      if(string.IsNullOrWhiteSpace(parentPath))
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

      var pathSegments = parentPath
        .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
      if(pathSegments.Length == 0)
      {
        continue;
      }

      AddFunctionToPath(rootVariables, pathSegments, fieldName, func);
    }

    foreach(var valueSymbol in providedValueSymbols)
    {
      var valueName = valueSymbol.Name;
      if(string.IsNullOrWhiteSpace(valueName))
      {
        continue;
      }

      var systemValue = ToSystemValue(valueSymbol);
      if(string.IsNullOrWhiteSpace(valueSymbol.ParentName))
      {
        rootVariables[valueName] = systemValue;
        continue;
      }

      var pathSegments = valueSymbol.ParentName
        .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
      if(pathSegments.Length == 0)
      {
        rootVariables[valueName] = systemValue;
        continue;
      }

      AddValueToPath(rootVariables, pathSegments, valueName, systemValue);
    }

    return rootVariables.Select(kv => new KeyValuePair<string, AresSystemValue>(kv.Key, kv.Value));
  }

  private static void AddFunctionToPath(
    IDictionary<string, AresSystemValue> root,
    string[] pathSegments,
    string fieldName,
    AresSystemFunctionSymbol func)
  {
    if(pathSegments.Length == 0)
    {
      var funcValue = AresSystemValue.Function(func);
      root[fieldName] = funcValue;
      return;
    }

    var segment = pathSegments[0];
    var remainingPath = pathSegments[1..];

    var valueExists = root.TryGetValue(segment, out var value);

    if(!valueExists || value is null)
    {
      var newRoot = AresSystemValue.Struct();
      root[segment] = newRoot;
      AddFunctionToPath(newRoot.StructFields!, remainingPath, fieldName, func);
      return;
    }

    if(value.ValueKind != AresSystemValue.AresSystemValueKind.Struct || value.StructFields is null)
    {
      throw new InvalidOperationException($"Value {segment} already exists and is not a struct, but rather a {value.ValueKind}");
    }

    AddFunctionToPath(value.StructFields, remainingPath, fieldName, func);
  }

  private static void AddValueToPath(
    IDictionary<string, AresSystemValue> root,
    string[] pathSegments,
    string fieldName,
    AresSystemValue valueToSet)
  {
    if(pathSegments.Length == 0)
    {
      root[fieldName] = valueToSet;
      return;
    }

    var segment = pathSegments[0];
    var remainingPath = pathSegments[1..];
    var valueExists = root.TryGetValue(segment, out var value);
    if(!valueExists || value is null)
    {
      var newRoot = AresSystemValue.Struct();
      root[segment] = newRoot;
      AddValueToPath(newRoot.StructFields!, remainingPath, fieldName, valueToSet);
      return;
    }

    if(value.ValueKind != AresSystemValue.AresSystemValueKind.Struct || value.StructFields is null)
    {
      throw new InvalidOperationException($"Value {segment} already exists and is not a struct, but rather a {value.ValueKind}");
    }

    AddValueToPath(value.StructFields, remainingPath, fieldName, valueToSet);
  }

  private static AresSystemValue ToSystemValue(IValueSymbol valueSymbol)
  {
    if(valueSymbol is AresSystemValue systemValueSymbol)
    {
      return systemValueSymbol;
    }

    return AresSystemValue.From(valueSymbol.Value);
  }
}
