using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Ares.Datamodel;
using Ares.Datamodel.Factories;
using AresScript.Generated;

namespace AresScript.ScriptAnalysis;

public sealed class VariableSchemaCollector : AresLangBaseVisitor<object?>
{
  private readonly AresTypeInferenceInterpreter _typeInference;
  private readonly Stack<Dictionary<string, SchemaEntry>> _scopes = new();
  private readonly Stack<IReadOnlyList<string>> _pendingFunctionParameters = new();
  private readonly int _line;
  private readonly int _column;
  private readonly string _identifier;

  public SchemaEntry? FoundSchema { get; private set; }

  public VariableSchemaCollector(
    AresScriptEnvironment env,
    IDictionary<string, SchemaEntry> globalSchemas,
    int line,
    int column,
    string identifier)
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

  public override object? VisitFunctionDecl([NotNull] AresLangParser.FunctionDeclContext context)
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

  public override object? VisitForStmt([NotNull] AresLangParser.ForStmtContext context)
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

  public override object? VisitId([NotNull] AresLangParser.IdContext context)
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

  public override object? VisitLValueId([NotNull] AresLangParser.LValueIdContext context)
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
