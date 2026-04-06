using Ares.Datamodel;
using Ares.Datamodel.Scripting;
using AresScript.Symbols;

namespace AresScript.Environment;

internal static class EnvironmentSymbolWriter
{
  public static void AddSystemSymbol(AresScriptEnvironment environment, IScriptSymbol symbol)
  {
    switch(symbol)
    {
      case AresSystemFunctionSymbol function:
        AddSystemFunction(environment, function);
        return;
      case IValueSymbol value:
        AddSystemValue(environment, value);
        return;
      default:
        throw new InvalidOperationException($"Unsupported system symbol type: {symbol.GetType().Name}");
    }
  }

  public static void AddSystemFunction(AresScriptEnvironment environment, AresSystemFunctionSymbol function)
  {
    if(environment.TryGetSystemFunction(function.Id, out _))
    {
      throw new InvalidOperationException($"System function '{function.Id}' already exists.");
    }

    var fieldName = string.IsNullOrWhiteSpace(function.Name) ? function.Id : function.Name;
    if(!string.IsNullOrWhiteSpace(fieldName))
    {
      var parentPath = !string.IsNullOrWhiteSpace(function.ParentName)
        ? function.ParentName
        : function.Namespace;

      var projectedFunction = AresSystemValue.Function(function) with
      {
        Name = fieldName,
        ParentName = NormalizePath(parentPath),
        IsReadOnly = true,
        SymbolKind = SymbolKind.Function,
        Detail = function.Detail,
        Documentation = function.Documentation
      };

      AddProjectedValue(environment, parentPath, fieldName, projectedFunction);
    }

    environment.AssignSystemFunctions([function]);
  }

  public static void AddSystemValue(AresScriptEnvironment environment, IValueSymbol valueSymbol)
  {
    if(string.IsNullOrWhiteSpace(valueSymbol.Name))
    {
      return;
    }

    AddProjectedValue(environment, valueSymbol.ParentName, valueSymbol.Name, NormalizeSystemValue(valueSymbol));
  }

  // Map a flat symbol path like devices.pump.run into the nested system-value tree.
  private static void AddProjectedValue(
    AresScriptEnvironment environment,
    string? parentPath,
    string fieldName,
    AresSystemValue value)
  {
    var pathSegments = string.IsNullOrWhiteSpace(parentPath) ? [] : SplitPath(parentPath);
    if(pathSegments.Length == 0)
    {
      AssignOrMergeRootValue(environment, fieldName, value);
      return;
    }

    var target = GetOrCreateStructAtPath(environment, pathSegments);
    AssignOrMergeValue(target, fieldName, value, BuildPath(BuildPath(pathSegments), fieldName));
  }

  private static IDictionary<string, AresSystemValue> GetOrCreateStructAtPath(
    AresScriptEnvironment environment,
    IReadOnlyList<string> pathSegments)
  {
    IDictionary<string, AresSystemValue>? current = null;
    string? iterParentPath = null;

    for(var index = 0; index < pathSegments.Count; index++)
    {
      var segment = pathSegments[index];
      var currentPath = iterParentPath is null ? segment : $"{iterParentPath}.{segment}";

      if(current is null)
      {
        if(!environment.TryGetSystemValueSymbol(segment, out var existingRoot))
        {
          var newRoot = CreateStructPlaceholder(segment, iterParentPath);
          environment.AssignSystemVariables([new KeyValuePair<string, AresSystemValue>(segment, newRoot)]);
          current = newRoot.StructFields;
          iterParentPath = currentPath;
          continue;
        }

        if(existingRoot.StructFields is null)
        {
          throw new InvalidOperationException($"System value '{currentPath}' already exists and is not a struct.");
        }

        current = existingRoot.StructFields;
        iterParentPath = currentPath;
        continue;
      }

      if(!current.TryGetValue(segment, out var child))
      {
        var next = CreateStructPlaceholder(segment, iterParentPath);
        current[segment] = next;
        current = next.StructFields!;
        iterParentPath = currentPath;
        continue;
      }

      if(child.StructFields is null)
      {
        throw new InvalidOperationException($"System value '{currentPath}' already exists and is not a struct.");
      }

      current = child.StructFields;
      iterParentPath = currentPath;
    }

    return current ?? throw new InvalidOperationException("A struct path is required.");
  }

  private static void AssignOrMergeRootValue(
    AresScriptEnvironment environment,
    string key,
    AresSystemValue value)
  {
    if(!environment.TryGetSystemValueSymbol(key, out var existing))
    {
      environment.AssignSystemVariables([new KeyValuePair<string, AresSystemValue>(key, value)]);
      return;
    }

    environment.AssignSystemVariables([new KeyValuePair<string, AresSystemValue>(key, MergeValues(existing, value, key))]);
  }

  private static void AssignOrMergeValue(
    IDictionary<string, AresSystemValue> target,
    string key,
    AresSystemValue value,
    string path)
  {
    if(!target.TryGetValue(key, out var existing))
    {
      target[key] = value;
      return;
    }

    target[key] = MergeValues(existing, value, path);
  }

  private static AresSystemValue MergeValues(AresSystemValue existing, AresSystemValue incoming, string path)
  {
    if(existing.StructFields is not null && incoming.StructFields is not null)
    {
      var mergedFields = new Dictionary<string, AresSystemValue>(existing.StructFields, StringComparer.Ordinal);
      foreach(var (childKey, childValue) in incoming.StructFields)
      {
        var childPath = BuildPath(path, childKey);
        if(mergedFields.TryGetValue(childKey, out var existingChild))
        {
          mergedFields[childKey] = MergeValues(existingChild, childValue, childPath);
        }
        else
        {
          mergedFields[childKey] = childValue;
        }
      }

      return AresSystemValue.Struct(mergedFields, incoming.SymbolKind != SymbolKind.Unspecified ? incoming.SymbolKind : existing.SymbolKind) with
      {
        Name = FirstNonEmpty(existing.Name, incoming.Name) ?? string.Empty,
        ParentName = FirstNonEmpty(existing.ParentName, incoming.ParentName),
        IsReadOnly = existing.IsReadOnly || incoming.IsReadOnly,
        IsUserDefined = existing.IsUserDefined || incoming.IsUserDefined,
        DeclaredSchema = existing.DeclaredSchema ?? incoming.DeclaredSchema,
        Detail = FirstNonEmpty(existing.Detail, incoming.Detail),
        Documentation = FirstNonEmpty(existing.Documentation, incoming.Documentation),
        SystemFunction = existing.SystemFunction ?? incoming.SystemFunction
      };
    }

    if(existing.Value.Equals(incoming.Value))
    {
      return existing with
      {
        Name = FirstNonEmpty(existing.Name, incoming.Name) ?? string.Empty,
        ParentName = FirstNonEmpty(existing.ParentName, incoming.ParentName),
        IsReadOnly = existing.IsReadOnly || incoming.IsReadOnly,
        IsUserDefined = existing.IsUserDefined || incoming.IsUserDefined,
        DeclaredSchema = existing.DeclaredSchema ?? incoming.DeclaredSchema,
        Detail = FirstNonEmpty(existing.Detail, incoming.Detail),
        Documentation = FirstNonEmpty(existing.Documentation, incoming.Documentation),
        SymbolKind = incoming.SymbolKind != SymbolKind.Unspecified ? incoming.SymbolKind : existing.SymbolKind,
        SystemFunction = existing.SystemFunction ?? incoming.SystemFunction
      };
    }

    throw new InvalidOperationException($"System value '{path}' already exists.");
  }

  private static AresSystemValue NormalizeSystemValue(IValueSymbol valueSymbol)
  {
    var asSystemValue = valueSymbol as AresSystemValue;
    return NormalizeAresValue(valueSymbol.Value) with
    {
      Name = valueSymbol.Name,
      ParentName = NormalizePath(valueSymbol.ParentName),
      IsReadOnly = valueSymbol.IsReadOnly,
      IsUserDefined = valueSymbol.IsUserDefined,
      SymbolKind = valueSymbol.SymbolKind,
      Detail = valueSymbol.Detail,
      Documentation = valueSymbol.Documentation,
      DeclaredSchema = asSystemValue?.DeclaredSchema,
      SystemFunction = asSystemValue?.SystemFunction
    };
  }

  private static AresSystemValue NormalizeAresValue(AresValue value)
  {
    return value.KindCase switch
    {
      AresValue.KindOneofCase.StructValue => AresSystemValue.Struct(
        value.StructValue.Fields.ToDictionary(
          field => field.Key,
          field => NormalizeAresValue(field.Value),
          StringComparer.Ordinal)),
      AresValue.KindOneofCase.ListValue => AresSystemValue.List(value.ListValue.Values.Select(NormalizeAresValue)),
      _ => AresSystemValue.From(value)
    };
  }

  private static AresSystemValue CreateStructPlaceholder(string name, string? parentPath)
  {
    return AresSystemValue.Struct() with
    {
      Name = name,
      ParentName = parentPath,
      IsReadOnly = true,
      SymbolKind = SymbolKind.Struct
    };
  }

  private static string[] SplitPath(string path)
  {
    return path
      .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
  }

  private static string? NormalizePath(string? path)
  {
    if(string.IsNullOrWhiteSpace(path))
    {
      return null;
    }

    var segments = SplitPath(path);
    return segments.Length == 0 ? null : string.Join('.', segments);
  }

  private static string BuildPath(IEnumerable<string> segments)
  {
    return string.Join('.', segments);
  }

  private static string BuildPath(string? parentPath, string child)
  {
    return string.IsNullOrWhiteSpace(parentPath) ? child : $"{parentPath}.{child}";
  }

  private static string? FirstNonEmpty(string? first, string? second)
  {
    if(!string.IsNullOrWhiteSpace(first))
    {
      return first;
    }

    return string.IsNullOrWhiteSpace(second) ? null : second;
  }
}
