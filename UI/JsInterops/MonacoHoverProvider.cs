using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Datamodel.Scripting;
using Ares.Services;
using AresScript;
using AresScript.Generated;
using Google.Protobuf.WellKnownTypes;
using Microsoft.JSInterop;

namespace UI.JsInterops;

public sealed class MonacoHoverProvider(AresScriptingService.AresScriptingServiceClient aresScriptingServiceClient)
{
  private readonly AresScriptingService.AresScriptingServiceClient _aresScriptingServiceClient = aresScriptingServiceClient;
  private AutocompleteCatalog? _cachedCatalog;

  [JSInvokable]
  public async Task<string?> GetHoverText(string script, int line, int column, string identifier)
  {
    if(string.IsNullOrWhiteSpace(identifier))
    {
      return null;
    }

    var request = new CompletionRequest
    {
      CursorColumn = column,
      CursorLine = line,
      Script = script
    };

    var completions = await _aresScriptingServiceClient.GetCompletionsAsync(request);
    var item = completions.Items.FirstOrDefault(i => string.Equals(i.Label, identifier, StringComparison.Ordinal));

    var markdown = item is null ? null : BuildHoverMarkdown(item);
    var inferredSchema = await TryGetInferredSchema(script, line, column, identifier);
    if(inferredSchema is not null)
    {
      markdown = AppendInferredSchema(markdown, identifier, inferredSchema);
    }

    return string.IsNullOrWhiteSpace(markdown) ? null : markdown;
  }

  private static string BuildHoverMarkdown(CompletionItem item)
  {
    var sb = new StringBuilder();
    var label = item.Label ?? string.Empty;
    var name = string.IsNullOrWhiteSpace(item.ParentIdentifier) ? label : $"{item.ParentIdentifier}.{label}";

    if(!string.IsNullOrWhiteSpace(name))
    {
      sb.Append("**").Append(name).Append("**");
    }

    AppendDescription(sb, item.Detail, item.Documentation);
    AppendDataSchemaSection(sb, "Inputs", item.InputSchema);
    AppendSchemaEntrySection(sb, "Output", item.OutputSchema);

    if(item.Schema is not null)
    {
      AppendSchemaEntrySection(sb, "Schema", item.Schema);
    }

    return sb.ToString();
  }

  private static void AppendDescription(StringBuilder sb, string? detail, string? documentation)
  {
    var trimmedDetail = detail?.Trim();
    var trimmedDoc = documentation?.Trim();

    if(string.IsNullOrWhiteSpace(trimmedDetail) && string.IsNullOrWhiteSpace(trimmedDoc))
    {
      return;
    }

    sb.AppendLine().AppendLine();

    if(!string.IsNullOrWhiteSpace(trimmedDetail))
    {
      sb.AppendLine(trimmedDetail);
    }

    if(!string.IsNullOrWhiteSpace(trimmedDoc) && !string.Equals(trimmedDoc, trimmedDetail, StringComparison.Ordinal))
    {
      if(!string.IsNullOrWhiteSpace(trimmedDetail))
      {
        sb.AppendLine();
      }

      sb.AppendLine(trimmedDoc);
    }
  }

  private static void AppendDataSchemaSection(StringBuilder sb, string title, AresDataSchema? schema)
  {
    if(schema is null || schema.Fields.Count == 0)
    {
      return;
    }

    sb.AppendLine().AppendLine();
    sb.Append("**").Append(title).AppendLine("**");
    sb.AppendLine("```text");

    foreach(var field in schema.Fields)
    {
      sb.Append(field.Key).Append(": ").AppendLine(FormatSchemaEntry(field.Value));
    }

    sb.AppendLine("```");
  }

  private static void AppendSchemaEntrySection(StringBuilder sb, string title, SchemaEntry entry)
  {
    sb.AppendLine().AppendLine();
    sb.Append("**").Append(title).AppendLine("**");
    sb.AppendLine("```text");
    if(entry.Type == AresDataType.Struct && entry.StructSchema is not null && entry.StructSchema.Fields.Count > 0)
    {
      foreach(var field in entry.StructSchema.Fields)
      {
        sb.Append(field.Key).Append(": ").AppendLine(FormatSchemaEntry(field.Value));
      }
    }
    else if(entry.Type == AresDataType.List && entry.ListElementSchema is not null)
    {
      sb.Append("List<").Append(FormatSchemaEntry(entry.ListElementSchema)).AppendLine(">");
    }
    else
    {
      sb.AppendLine(FormatSchemaEntry(entry));
    }
    sb.AppendLine("```");
  }

  private static string FormatSchemaEntry(SchemaEntry entry)
  {
    var typeName = entry.Type.ToString();
    return entry.Optional ? $"{typeName} (optional)" : typeName;
  }

  private async Task<SchemaEntry?> TryGetInferredSchema(string script, int line, int column, string identifier)
  {
    if(string.IsNullOrWhiteSpace(script))
    {
      return null;
    }

    var catalog = await GetAutocompleteCatalog();
    if(catalog is null)
    {
      return null;
    }

    var env = BuildEnvironmentForInference(catalog);
    var program = TryParseProgram(script);
    if(program is null)
    {
      return null;
    }

    var globalSchemas = new Dictionary<string, SchemaEntry>(StringComparer.Ordinal);
    foreach(var global in catalog.Globals)
    {
      if(global.Schema is not null && !string.IsNullOrWhiteSpace(global.Name))
      {
        globalSchemas[global.Name] = global.Schema;
      }
    }

    var collector = new VariableSchemaCollector(env, globalSchemas, line, column, identifier);
    collector.Visit(program);
    return collector.FoundSchema;
  }

  private async Task<AutocompleteCatalog?> GetAutocompleteCatalog()
  {
    if(_cachedCatalog is not null)
    {
      return _cachedCatalog;
    }

    var response = await _aresScriptingServiceClient.GetAutocompleteCatalogAsync(new Empty());
    _cachedCatalog = response.Catalog;
    return _cachedCatalog;
  }

  private static AresLangParser.ProgramContext? TryParseProgram(string script)
  {
    try
    {
      var stream = new AntlrInputStream(script);
      var lexer = new AresIndentationLexer(stream);
      var tokenStream = new CommonTokenStream(lexer);
      var parser = new AresLangParser(tokenStream);
      return parser.program();
    }
    catch
    {
      return null;
    }
  }

  private static AresScriptEnvironment BuildEnvironmentForInference(AutocompleteCatalog catalog)
  {
    var env = new AresScriptEnvironment();
    var functions = catalog.GlobalFunctions
      .Select(func => CreateSystemFunction(func, string.Empty))
      .Concat(catalog.Namespaces.SelectMany(ns => ns.Functions.Select(func => CreateSystemFunction(func, ns.Identifier))))
      .ToArray();

    env.AssignSystemFunctions(functions);
    env.AssignSystemVariables(BuildNamespaceVariables(functions));
    return env;
  }

  private static AresSystemFunction CreateSystemFunction(FunctionSymbol function, string namespaceName)
  {
    var id = string.IsNullOrWhiteSpace(function.Id) ? function.Name : function.Id;
    var name = string.IsNullOrWhiteSpace(function.Name) ? id : function.Name;
    var inputSchema = function.InputSchema ?? new AresDataSchema();
    var outputSchema = function.OutputSchema ?? new SchemaEntry();
    var description = function.Description ?? string.Empty;
    return new AresSystemFunction(id, name, DummyFunction, inputSchema, outputSchema, namespaceName, description);
  }

  private static Task<AresValue> DummyFunction(List<AresValue> _, CancellationToken __)
  {
    return Task.FromResult(AresValueHelper.CreateNull());
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

  private static string AppendInferredSchema(string? markdown, string identifier, SchemaEntry schema)
  {
    var sb = new StringBuilder();
    if(!string.IsNullOrWhiteSpace(markdown))
    {
      sb.Append(markdown);
    }
    else
    {
      sb.Append("**").Append(identifier).Append("**");
    }

    AppendSchemaEntrySection(sb, "Inferred Type", schema);
    return sb.ToString();
  }

  private sealed class VariableSchemaCollector : AresLangBaseVisitor<object?>
  {
    private readonly AresTypeInferenceInterpreter _typeInference;
    private readonly Stack<Dictionary<string, SchemaEntry>> _scopes = new();
    private readonly Stack<IReadOnlyList<string>> _pendingFunctionParameters = new();
    private readonly int _line;
    private readonly int _column;
    private readonly string _identifier;

    public SchemaEntry? FoundSchema { get; private set; }

    public VariableSchemaCollector(AresScriptEnvironment env, IDictionary<string, SchemaEntry> globalSchemas, int line, int column, string identifier)
    {
      _typeInference = new AresTypeInferenceInterpreter(env);
      _scopes.Push(new Dictionary<string, SchemaEntry>(globalSchemas, StringComparer.Ordinal));
      _line = line;
      _column = column;
      _identifier = identifier;
    }

    public override object? VisitAssignStmt(AresLangParser.AssignStmtContext context)
    {
      var assignment = context.assignment();
      if(assignment is null)
      {
        return null;
      }

      if(assignment.lvalue() is AresLangParser.LValueIdContext idContext && assignment.expression() is not null)
      {
        var id = idContext.ID().GetText();
        var schema = _typeInference.Visit(assignment.expression());
        _scopes.Peek()[id] = schema;
      }

      return base.VisitAssignStmt(context);
    }

    public override object? VisitFunctionDecl(AresLangParser.FunctionDeclContext context)
    {
      var decl = context.functionDeclaration();
      if(decl is not null)
      {
        var ids = decl.ID();
        var parameters = new List<string>();
        for(var i = 1; i < ids.Length; i++)
        {
          var id = ids[i].GetText();
          if(!string.IsNullOrWhiteSpace(id))
          {
            parameters.Add(id);
          }
        }

        _pendingFunctionParameters.Push(parameters);
      }

      try
      {
        return base.VisitFunctionDecl(context);
      }
      finally
      {
        if(_pendingFunctionParameters.Count > 0)
        {
          _pendingFunctionParameters.Pop();
        }
      }
    }

    public override object? VisitForStmt(AresLangParser.ForStmtContext context)
    {
      var stmt = context.forStatement();
      if(stmt is null)
      {
        return null;
      }

      var id = stmt.ID();
      if(id is null)
      {
        return null;
      }

      var matchesHover = TryResolveTokenHover(id.Symbol)
        && string.Equals(id.GetText(), _identifier, StringComparison.Ordinal);
      Visit(stmt.expression());
      PushScope();
      try
      {
        _scopes.Peek()[id.GetText()] = AresSchemaBuilder.Entry(AresDataType.Any).Build();
        if(matchesHover && FoundSchema is null)
        {
          FoundSchema = ResolveSchema(id.GetText());
        }
        Visit(stmt.loopBlock());
      }
      finally
      {
        PopScope();
      }

      return null;
    }

    public override object? VisitFuncBlock(AresLangParser.FuncBlockContext context)
    {
      PushScope();
      try
      {
        if(_pendingFunctionParameters.Count > 0)
        {
          foreach(var parameter in _pendingFunctionParameters.Peek())
          {
            _scopes.Peek()[parameter] = AresSchemaBuilder.Entry(AresDataType.Any).Build();
          }
        }

        return base.VisitFuncBlock(context);
      }
      finally
      {
        PopScope();
      }
    }

    public override object? VisitId(AresLangParser.IdContext context)
    {
      if(FoundSchema is not null)
      {
        return null;
      }

      var token = context.ID().Symbol;
      if(!TryResolveTokenHover(token))
      {
        return null;
      }

      var id = context.ID().GetText();
      if(!string.Equals(id, _identifier, StringComparison.Ordinal))
      {
        return null;
      }

      FoundSchema = ResolveSchema(id);
      return null;
    }

    public override object? VisitLValueId(AresLangParser.LValueIdContext context)
    {
      if(FoundSchema is not null)
      {
        return null;
      }

      if(!TryResolveTokenHover(context.ID().Symbol))
      {
        return null;
      }

      var id = context.ID().GetText();
      if(!string.Equals(id, _identifier, StringComparison.Ordinal))
      {
        return null;
      }

      FoundSchema = ResolveSchema(id);
      return null;
    }

    private void PushScope()
    {
      _scopes.Push(new Dictionary<string, SchemaEntry>(StringComparer.Ordinal));
    }

    private void PopScope()
    {
      _scopes.Pop();
    }

    private SchemaEntry? ResolveSchema(string id)
    {
      foreach(var scope in _scopes)
      {
        if(scope.TryGetValue(id, out var schema))
        {
          return schema;
        }
      }

      return null;
    }

    private bool TryResolveTokenHover(IToken? token)
    {
      if(FoundSchema is not null || token is null || token.Line != _line)
      {
        return false;
      }

      var startColumn = token.Column + 1;
      var endColumn = startColumn + (token.Text?.Length ?? 0) - 1;
      return _column >= startColumn && _column <= endColumn;
    }
  }
}
