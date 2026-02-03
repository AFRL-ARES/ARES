using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using AresScript.Generated;
using Google.Protobuf.WellKnownTypes;

namespace AresScript.Interpreters;

/// <summary>
/// Interpreter specifically to validate the script in order to provide underline support and
/// pre-run validation to make sure we check as many things as possible before running
/// </summary>
public sealed class AresValidationInterpreter : AresLangBaseVisitor<Task>
{
  private readonly AresScriptEnvironment _environment;
  private int _functionDepth;
  private readonly int? _line = null;
  private readonly ValidationMode _mode;
  private readonly Stack<string[]> _pendingFunctionParameters = new();
  private readonly AresTypeInferenceInterpreter _typeInference;
  
  private sealed class StopTraversalException : Exception { }

  /// <summary>
  /// Multipurpose interpreter to catch any script errors before execution as well as to help build up the environment for autocomplete.
  /// Could be different interpreters, but that's a lot of code duplication.
  /// </summary>
  /// <param name="aresScriptEnvironment">The base environment to build upon</param>
  /// <param name="mode">When mode is strict, exceptions are thrown when there's a script error and is used for validation.
  /// Lenient mode is used for building up the environment for autocomplete.</param>
  /// <param name="line">Optional line number used for autocomplete support. If line is within a function block, we'll stop traversal
  /// so that the environment can contain the parameter variable names from the function definition parameters.
  /// Basically you should only use this parameter when working with completions.</param>
  public AresValidationInterpreter(AresScriptEnvironment aresScriptEnvironment, ValidationMode mode = ValidationMode.Strict, int? line = null)
  {
    _environment = aresScriptEnvironment;
    _mode = mode;
    _typeInference = new AresTypeInferenceInterpreter(_environment);
    _line = line;
  }

  protected override Task DefaultResult => Task.CompletedTask;

  public override async Task VisitProgram(AresLangParser.ProgramContext context)
  {
    try
    {
      foreach(var child in context.children)
      {
        if(child is AresLangParser.StatementContext stmt)
        {
          if(ShouldStopBefore(stmt.Start))
          {
            throw new StopTraversalException();
          }

          await Visit(stmt);

          if(ShouldStopWithin(stmt.Start, stmt.Stop))
          {
            throw new StopTraversalException();
          }
        }
      }
    }
    catch(StopTraversalException)
    {
    }
  }

  public override async Task VisitBlock(AresLangParser.BlockContext context)
  {
    foreach(var stmt in context.statement())
    {
      if(ShouldStopBefore(stmt.Start))
      {
        throw new StopTraversalException();
      }

      await Visit(stmt);

      if(ShouldStopWithin(stmt.Start, stmt.Stop))
      {
        throw new StopTraversalException();
      }
    }
  }

  public override async Task VisitLoopBlock(AresLangParser.LoopBlockContext context)
  {
    foreach(var stmt in context.statement())
    {
      if(ShouldStopBefore(stmt.Start))
      {
        throw new StopTraversalException();
      }

      await Visit(stmt);

      if(ShouldStopWithin(stmt.Start, stmt.Stop))
      {
        throw new StopTraversalException();
      }
    }
  }

  public override async Task VisitFuncBlock(AresLangParser.FuncBlockContext context)
  {
    var hasBodySpan = TryGetBodySpan(context, out var bodyStartLine, out var bodyStopLine);
    if(_line is not null && (!hasBodySpan || _line.Value < bodyStartLine || _line.Value > bodyStopLine))
    {
      return;
    }

    _environment.EnterScope();
    _functionDepth++;
    try
    {
      if(_pendingFunctionParameters.Count > 0)
      {
        foreach(var parameter in _pendingFunctionParameters.Peek())
        {
          _environment.AssignVariable(parameter, AresValueHelper.CreateNull());
        }
      }

      foreach(var stmt in context.statement())
      {
        if(ShouldStopBefore(stmt.Start))
        {
          throw new StopTraversalException();
        }

        await Visit(stmt);

        if(ShouldStopWithin(stmt.Start, stmt.Stop))
        {
          throw new StopTraversalException();
        }
      }
    }
    finally
    {
      if(_line is null || !hasBodySpan || _line.Value < bodyStartLine || _line.Value > bodyStopLine)
      {
        _functionDepth--;
        _environment.ExitScope();
      }
    }
  }

  public override async Task VisitParallelBlock([NotNull] AresLangParser.ParallelBlockContext context)
  {
    if(ShouldStopBefore(context.Start))
    {
      throw new StopTraversalException();
    }

    var expContexts = context.expression();
    var expTasks = expContexts.Select(Visit);
    await Task.WhenAll(expTasks);

    if(ShouldStopWithin(context.Start, context.Stop))
    {
      throw new StopTraversalException();
    }
  }

  public override async Task VisitAssignStmt(AresLangParser.AssignStmtContext context)
  {
    var assignment = context.assignment();
    var lvalue = assignment.lvalue();
    var expr = assignment.expression();
    if(lvalue is null || expr is null)
    {
      if(_mode == ValidationMode.Strict)
      {
        throw new AresInterpreterException("Incomplete assignment.", context.Start.Line, context.Start.Column);
      }

      return;
    }

    await Visit(lvalue);
    await Visit(expr);

    if(lvalue is AresLangParser.LValueIdContext idContext)
    {
      var id = idContext.ID().GetText();
      var assignedValue = TryBuildAssignmentValue(expr);
      if(assignedValue is not null)
      {
        _environment[id] = assignedValue;
      }
    }
    else if(lvalue is AresLangParser.LValueMemberContext memberContext)
    {
      if(!TryResolveLValue(memberContext.lvalue(), out var baseValue) || baseValue?.StructValue is null)
      {
        if(_mode == ValidationMode.Strict)
        {
          throw new AresInterpreterException(
            $"Unknown identifier '{memberContext.lvalue().GetText()}'.",
            context.Start.Line,
            context.Start.Column
          );
        }

        return;
      }

      var memberId = memberContext.ID().GetText();
      var assignedValue = TryBuildAssignmentValue(expr) ?? AresValueHelper.CreateNull();
      baseValue.StructValue.Fields[memberId] = assignedValue;
    }
    else if(lvalue is AresLangParser.LValueIndexContext indexContext)
    {
      if(!TryResolveLValue(indexContext.lvalue(), out var baseValue) || baseValue?.StructValue is null)
      {
        if(_mode == ValidationMode.Strict)
        {
          throw new AresInterpreterException(
            $"Unknown identifier '{indexContext.lvalue().GetText()}'.",
            context.Start.Line,
            context.Start.Column
          );
        }

        return;
      }

      var indexValue = TryBuildAssignmentValue(indexContext.expression());
      if(indexValue?.HasStringValue != true)
      {
        if(_mode == ValidationMode.Strict)
        {
          throw new AresInterpreterException(
            "Provided index expression was not a string.",
            context.Start.Line,
            context.Start.Column
          );
        }

        return;
      }

      var assignedValue = TryBuildAssignmentValue(expr) ?? AresValueHelper.CreateNull();
      baseValue.StructValue.Fields[indexValue.StringValue] = assignedValue;
    }
  }

  public override async Task VisitExprStmt(AresLangParser.ExprStmtContext context)
  {
    var expr = context.expression();
    if(expr is null)
    {
      if(_mode == ValidationMode.Strict)
      {
        throw new AresInterpreterException("Incomplete expression.", context.Start.Line, context.Start.Column);
      }

      return;
    }

    await Visit(expr);
  }

  public override async Task VisitReturnStmt(AresLangParser.ReturnStmtContext context)
  {
    if(context.expression() is not null)
    {
      await Visit(context.expression());
    }
  }

  public override async Task VisitAssertStmt(AresLangParser.AssertStmtContext context)
  {
    var assertContext = context.assertStatement();
    var expressions = assertContext.expression();
    if(expressions.Length == 0)
    {
      if(_mode == ValidationMode.Strict)
      {
        throw new AresInterpreterException("Incomplete assert statement.", context.Start.Line, context.Start.Column);
      }

      return;
    }

    foreach(var expr in expressions)
    {
      await Visit(expr);
    }
  }

  public override async Task VisitWhileStmt([NotNull] AresLangParser.WhileStmtContext context)
  {
    var stmt = context.whileStatement();
    if(stmt is null)
    {
      return;
    }

    var condition = stmt.expression();
    var block = stmt.loopBlock();
    if(condition is null || block is null)
    {
      if(_mode == ValidationMode.Strict)
      {
        throw new AresInterpreterException("Incomplete while statement.", context.Start.Line, context.Start.Column);
      }

      return;
    }

    await Visit(condition);
    await Visit(block);
  }

  public override async Task VisitForStmt([NotNull] AresLangParser.ForStmtContext context)
  {
    var stmt = context.forStatement();
    if(stmt is null)
    {
      return;
    }

    var id = stmt.ID();
    var expression = stmt.expression();
    var block = stmt.loopBlock();
    if(id is null || expression is null || block is null)
    {
      if(_mode == ValidationMode.Strict)
      {
        throw new AresInterpreterException("Incomplete for statement.", context.Start.Line, context.Start.Column);
      }

      return;
    }

    await Visit(expression);
    _environment.EnterScope();
    try
    {
      _environment.AssignVariable(id.GetText(), AresValueHelper.CreateNull());
      await Visit(block);
    }
    finally
    {
      _environment.ExitScope();
    }
  }

  public override async Task VisitIfStmt([NotNull] AresLangParser.IfStmtContext context)
  {
    var stmt = context.ifStatement();
    if(stmt is null)
    {
      return;
    }

    var expressions = stmt.expression();
    var blocks = stmt.block();
    if(expressions.Length == 0 || blocks.Length == 0)
    {
      if(_mode == ValidationMode.Strict)
      {
        throw new AresInterpreterException("Incomplete if statement.", context.Start.Line, context.Start.Column);
      }

      return;
    }

    if(blocks.Length < expressions.Length || blocks.Length > expressions.Length + 1)
    {
      if(_mode == ValidationMode.Strict)
      {
        throw new AresInterpreterException("Incomplete if statement.", context.Start.Line, context.Start.Column);
      }

      return;
    }

    for(var i = 0; i < expressions.Length; i++)
    {
      await Visit(expressions[i]);
      await Visit(blocks[i]);
    }

    if(blocks.Length > expressions.Length)
    {
      await Visit(blocks[^1]);
    }
  }

  public override async Task VisitFunctionDecl([NotNull] AresLangParser.FunctionDeclContext context)
  {
    var decl = context.functionDeclaration();
    var ids = decl.ID();
    if(ids.Length == 0)
    {
      if(_mode == ValidationMode.Strict)
      {
        throw new AresInterpreterException("Incomplete function declaration.", context.Start.Line, context.Start.Column);
      }
      return;
    }

    var functionId = ids[0].GetText();
    var paramIds = ids.Skip(1).Select(p => p.GetText()).ToArray();
    var block = decl.funcBlock();
    if(block is null)
    {
      if(_mode == ValidationMode.Strict)
      {
        throw new AresInterpreterException("Incomplete function declaration.", context.Start.Line, context.Start.Column);
      }
      return;
    }

    var userFunc = new AresScriptFunction(functionId, paramIds, block);
    _environment.AssignFunction(functionId, userFunc);

    if(_line is not null && _line.Value == context.Start.Line)
    {
      return;
    }

    _pendingFunctionParameters.Push(paramIds);
    
    try
    {
      await Visit(block);
    }
    finally
    {
      _pendingFunctionParameters.Pop();
    }
  }

  public override Task VisitId([NotNull] AresLangParser.IdContext context)
  {
    var id = context.ID().GetText();
    if(_environment.TryGetValue(id, out _)
      || _environment.TryGetSystemFunction(id, out _)
      || _environment.TryGetUserFunction(id, out _))
    {
      return Task.CompletedTask;
    }

    if(_mode == ValidationMode.Strict)
    {
      throw new AresInterpreterException($"Unknown identifier '{id}'.", context.Start.Line, context.Start.Column);
    }

    return Task.FromResult("");
  }

  public override Task VisitMemberAccess([NotNull] AresLangParser.MemberAccessContext context)
  {
    var ctxExpr = context.expression();
    if(!TryResolveValue(ctxExpr, out var value))
    {
      throw new AresInterpreterException(
        $"Unknown identifier '{ctxExpr.GetText()}'.",
        ctxExpr.Start.Line,
        ctxExpr.Start.Column
      );
    }

    if(value?.KindCase == AresValue.KindOneofCase.StructValue)
    {
      if(value.StructValue.Fields.ContainsKey(context.ID().GetText()))
      {
        return Task.CompletedTask;
      }
    }

    throw new AresInterpreterException(
      $"Unknown identifier {context.ID().GetText()} on {ctxExpr.GetText()}.",
      context.Start.Line,
      context.Stop.Column + 1
    );
  }

  public override async Task VisitFunctionCall(AresLangParser.FunctionCallContext ctx)
  {
    await Visit(ctx.expression());

    var positionalArgs = new List<AresLangParser.ExpressionContext>();
    var keywordArgs = new Dictionary<string, AresLangParser.ExpressionContext>(StringComparer.Ordinal);
    var seenKeywordArg = false;

    var argContexts = ctx.argList()?.argument() ?? Enumerable.Empty<AresLangParser.ArgumentContext>();
    foreach(var argCtx in argContexts)
    {
      switch(argCtx)
      {
        case AresLangParser.PositionalArgContext positionalArg:
          {
            if(seenKeywordArg)
            {
              throw new AresInterpreterException(
                "Positional argument follows keyword argument.",
                positionalArg.Start.Line,
                positionalArg.Start.Column
              );
            }

            positionalArgs.Add(positionalArg.expression());
            await Visit(positionalArg.expression());
            break;
          }
        case AresLangParser.KeywordArgContext keywordArg:
          {
            seenKeywordArg = true;
            var name = keywordArg.ID().GetText();
            if(keywordArgs.ContainsKey(name))
            {
              throw new AresInterpreterException(
                $"Duplicate keyword argument '{name}'.",
                keywordArg.Start.Line,
                keywordArg.Start.Column
              );
            }

            keywordArgs[name] = keywordArg.expression();
            await Visit(keywordArg.expression());
            break;
          }
        default:
          throw new AresInterpreterException(
            $"Unsupported argument type {argCtx.GetType().Name}.",
            argCtx.Start.Line,
            argCtx.Start.Column
          );
      }
    }

    var functionId = TryResolveFunctionId(ctx.expression());
    if(functionId is null)
    {
      var ctxExpr = ctx.expression();
      if(ctxExpr is AresLangParser.MemberAccessContext memberCtx)
      {
        var idExpr = memberCtx.ID();
        throw new AresInterpreterException(
          $"Unknown function '{idExpr.GetText()}' on '{memberCtx.expression().GetText()}'.",
          memberCtx.Start.Line,
          memberCtx.Stop.Column + 1
        );
      }
      throw new AresInterpreterException(
        $"Unknown function '{ctxExpr.GetText()}'.",
        ctxExpr.Start.Line,
        ctxExpr.Start.Column
      );
    }

    if(_environment.TryGetValue(functionId, out var aliasValue) && aliasValue.FunctionValue is not null)
    {
      functionId = aliasValue.FunctionValue.FunctionId;
    }

    if(_environment.TryGetSystemFunction(functionId, out var systemFn))
    {
      if(keywordArgs.Count > 0)
      {
        throw new AresInterpreterException($"Runtime function '{functionId}' does not support keyword arguments");
      }

      ValidateSystemFunctionArgs(systemFn, positionalArgs, keywordArgs, ctx);
      return;
    }

    if(_environment.TryGetUserFunction(functionId, out var userFn))
    {
      if(positionalArgs.Count > userFn.Parameters.Count)
      {
        throw new AresInterpreterException(
          $"Function '{functionId}' expected {userFn.Parameters.Count} arguments but got {positionalArgs.Count}"
        );
      }

      foreach(var (name, _) in keywordArgs)
      {
        var index = FindParameterIndex(userFn.Parameters, name);
        if(index < 0)
        {
          throw new AresInterpreterException($"Function '{functionId}' got an unexpected keyword argument '{name}'");
        }

        if(index < positionalArgs.Count)
        {
          throw new AresInterpreterException($"Function '{functionId}' got multiple values for argument '{name}'");
        }
      }

      for(var i = positionalArgs.Count; i < userFn.Parameters.Count; i++)
      {
        var name = userFn.Parameters[i];
        if(!keywordArgs.ContainsKey(name))
        {
          throw new AresInterpreterException(
            $"Function '{functionId}' missing required argument '{name}'.",
            ctx.Start.Line,
            ctx.Start.Column
          );
        }
      }

      return;
    }

    if(_functionDepth == 0)
    {
      throw new AresInterpreterException($"Function '{functionId}' not found.", ctx.Start.Line, ctx.Start.Column);
    }
  }

  private void ValidateSystemFunctionArgs(AresSystemFunction function, IReadOnlyList<AresLangParser.ExpressionContext> positionalArgs, IReadOnlyDictionary<string, AresLangParser.ExpressionContext> keywordArgs, AresLangParser.FunctionCallContext ctx)
  {
    var schema = function.InputSchema;
    if(schema.Fields.Count == 0 && keywordArgs.Count == 0)
    {
      return;
    }

    foreach(var (name, expr) in keywordArgs)
    {
      if(!schema.Fields.TryGetValue(name, out var expected))
      {
        throw new AresInterpreterException(
          $"Function '{function.Id}' got an unexpected keyword argument '{name}'.",
          ctx.Start.Line,
          ctx.Start.Column
        );
      }

      var actual = _typeInference.Visit(expr);
      if(!IsCompatible(expected, actual))
      {
        throw new AresInterpreterException(
          $"Function '{function.Id}' argument '{name}' type mismatch. Expected {expected.Type}, received {actual.Type}.",
          ctx.Start.Line,
          ctx.Start.Column
        );
      }
    }

    if(keywordArgs.Count == 0 && positionalArgs.Count == schema.Fields.Count && schema.Fields.Count > 0)
    {
      var expectedNames = schema.Fields.Keys.ToArray();
      var expectedEntries = schema.Fields.Values.ToArray();
      for(var i = 0; i < positionalArgs.Count; i++)
      {
        var name = expectedNames[i];
        var expected = expectedEntries[i];
        var actual = _typeInference.Visit(positionalArgs[i]);
        if(!IsCompatible(expected, actual))
        {
          throw new AresInterpreterException(
            $"Function '{function.Id}' argument '{name}' type mismatch. Expected {expected.Type}, received {actual.Type}.",
            ctx.Start.Line,
            ctx.Start.Column
          );
        }
      }
    }
  }

  private static bool IsCompatible(SchemaEntry expected, SchemaEntry actual)
  {
    if(expected.Type == AresDataType.Any || expected.Type == AresDataType.UnspecifiedType)
    {
      return true;
    }

    if(actual.Type == AresDataType.Any || actual.Type == AresDataType.UnspecifiedType)
    {
      return true;
    }

    if(expected.Optional && actual.Type == AresDataType.Null)
    {
      return true;
    }

    if(expected.Type == actual.Type)
    {
      return true;
    }

    return false;
  }

  private static int FindParameterIndex(IReadOnlyList<string> parameters, string name)
  {
    for(var i = 0; i < parameters.Count; i++)
    {
      if(string.Equals(parameters[i], name, StringComparison.Ordinal))
        return i;
    }

    return -1;
  }

  private string? TryResolveFunctionId(AresLangParser.ExpressionContext expression)
  {
    if(TryResolveValue(expression, out var value) && value?.FunctionValue is not null)
    {
      return value.FunctionValue.FunctionId;
    }

    if(expression is AresLangParser.AtomExprContext atomExpr)
    {
      switch(atomExpr.atom())
      {
        case AresLangParser.IdContext id:
          return id.ID().GetText();
        case AresLangParser.ParensContext parens:
          return TryResolveFunctionId(parens.expression());
      }
    }
    else if(expression is AresLangParser.MemberAccessContext memberCtx)
    {
      return memberCtx.ID()?.GetText();
    }

    return null;
  }

  private AresValue? TryBuildAssignmentValue(AresLangParser.ExpressionContext expression)
  {
    var functionId = TryResolveFunctionId(expression);
    if(functionId is not null)
    {
      if(_environment.TryGetSystemFunction(functionId, out var _) || _environment.TryGetUserFunction(functionId, out var _))
      {
        return AresValueHelper.CreateFunction(functionId);
      }

      if(_environment.TryGetValue(functionId, out var value) && value.FunctionValue is not null)
      {
        return AresValueHelper.CreateFunction(value.FunctionValue.FunctionId);
      }
    }

    if(TryResolveValue(expression, out var envValue))
    {
      return envValue;
    }

    switch(expression)
    {
      case AresLangParser.AtomExprContext atomCtx:
        {
          var atomNode = atomCtx.atom();
          switch(atomNode)
          {
            case AresLangParser.StructContext structContext:
              {
                var aresStruct = AresValueHelper.CreateStruct();
                foreach(var structMember in structContext.structure().pair())
                {
                  var key = structMember.ID()?.GetText() ?? InterpreterHelpers.Unquote(structMember.STRING().GetText());
                  aresStruct.StructValue.Fields[key] = AresValueHelper.CreateNull();
                }

                return aresStruct;
              }
            case AresLangParser.IntContext intContext:
              if(int.TryParse(intContext.INT().GetText(), out var intValue))
              {
                return AresValueHelper.CreateNumber(intValue);
              }
              break;
            case AresLangParser.FloatContext floatContext:
              if(double.TryParse(floatContext.FLOAT().GetText(), out var doubleValue))
              {
                return AresValueHelper.CreateNumber(doubleValue);
              }
              break;
            case AresLangParser.StringContext stringContext:
              return AresValueHelper.CreateString(InterpreterHelpers.Unquote(stringContext.STRING().GetText()));
            case AresLangParser.BoolContext boolContext:
              return AresValueHelper.CreateBool(
              boolContext.BOOL().GetText().Equals("true", StringComparison.OrdinalIgnoreCase)
              );
            case AresLangParser.NoneContext:
              return AresValueHelper.CreateNull();
            case AresLangParser.ArrayContext arrayContext:
              {
                var expressions = arrayContext.expression();
                if(expressions.Length == 0)
                {
                  return AresValueHelper.CreateList();
                }

                var values = expressions
                  .Select(exp => TryBuildAssignmentValue(exp) ?? AresValueHelper.CreateNull())
                  .ToList();

                var initialKind = values[0].KindCase;
                var sameKind = values.All(v => v.KindCase == initialKind);
                if(sameKind && initialKind == AresValue.KindOneofCase.NumberValue)
                {
                  return AresValueHelper.CreateNumberArray(values.Select(v => v.NumberValue).ToArray());
                }

                if(sameKind && initialKind == AresValue.KindOneofCase.StringValue)
                {
                  return AresValueHelper.CreateStringArray(values.Select(v => v.StringValue).ToArray());
                }

                return AresValueHelper.CreateList(values);
              }
            case AresLangParser.ParensContext parensContext:
              return TryBuildAssignmentValue(parensContext.expression());
          }
          break;
        }
      case AresLangParser.FunctionCallContext functionCallContext:
        {
          var funcId = TryResolveFunctionId(functionCallContext.expression());
          if(funcId is not null && _environment.TryGetSystemFunction(funcId, out var systemFunction))
          {
            var schema = systemFunction.OutputSchema;
            var dummyValue = InterpreterHelpers.CreateDummyValue(schema);
            return dummyValue;
          }
          break;
        }
    }

    return null;
  }

  private bool TryResolveValue(AresLangParser.ExpressionContext expression, out AresValue? value)
  {
    value = AresValueHelper.CreateNull();

    if(expression is AresLangParser.AtomExprContext atomExpr)
    {
      if(atomExpr.atom() is AresLangParser.IdContext id)
      {
        return _environment.TryGetValue(id.ID().GetText(), out value);
      }
    }

    if(expression is AresLangParser.MemberAccessContext memberAccess)
    {
      if(TryResolveValue(memberAccess.expression(), out var baseValue)
        && baseValue?.StructValue is not null
        && baseValue.StructValue.Fields.TryGetValue(memberAccess.ID().GetText(), out var member))
      {
        value = member;
        return true;
      }
    }

    if(expression is AresLangParser.IndexAccessContext indexAccess)
    {
      if(TryResolveValue(indexAccess.expression(0), out var baseValue)
        && baseValue?.StructValue is not null)
      {
        var indexValue = TryBuildAssignmentValue(indexAccess.expression(1));
        if(indexValue?.HasStringValue == true
          && baseValue.StructValue.Fields.TryGetValue(indexValue.StringValue, out var member))
        {
          value = member;
          return true;
        }
      }
    }

    return false;
  }

  private bool TryResolveLValue(AresLangParser.LvalueContext lvalue, out AresValue? value)
  {
    value = null;

    switch(lvalue)
    {
      case AresLangParser.LValueIdContext idContext:
        return _environment.TryGetUserValue(idContext.ID().GetText(), out value);
      case AresLangParser.LValueMemberContext memberContext:
        if(!TryResolveLValue(memberContext.lvalue(), out var baseValue) || baseValue?.StructValue is null)
        {
          return false;
        }

        var memberId = memberContext.ID().GetText();
        if(!baseValue.StructValue.Fields.TryGetValue(memberId, out var memberValue))
        {
          memberValue = AresValueHelper.CreateNull();
          baseValue.StructValue.Fields[memberId] = memberValue;
        }

        value = memberValue;
        return true;
      case AresLangParser.LValueIndexContext indexContext:
        if(!TryResolveLValue(indexContext.lvalue(), out var baseValue2) || baseValue2?.StructValue is null)
        {
          return false;
        }

        var indexValue = TryBuildAssignmentValue(indexContext.expression());
        if(indexValue?.HasStringValue != true)
        {
          return false;
        }

        if(!baseValue2.StructValue.Fields.TryGetValue(indexValue.StringValue, out var indexedMember))
        {
          indexedMember = AresValueHelper.CreateNull();
          baseValue2.StructValue.Fields[indexValue.StringValue] = indexedMember;
        }

        value = indexedMember;
        return true;
      default:
        return false;
    }
  }

  private bool ShouldStopBefore(IToken? start)
  {
    if(_line is null || start is null)
    {
      return false;
    }

    return _line.Value < start.Line;
  }

  private bool ShouldStopWithin(IToken? start, IToken? stop)
  {
    if(_line is null)
    {
      return false;
    }

    return IsLineWithin(start, stop);
  }

  private bool IsLineWithin(IToken? start, IToken? stop)
  {
    if(_line is null || start is null || stop is null)
    {
      return false;
    }

    return _line.Value >= start.Line && _line.Value <= stop.Line;
  }

  private static bool TryGetBodySpan(AresLangParser.FuncBlockContext context, out int startLine, out int stopLine)
  {
    var statements = context.statement();
    if(statements is null || statements.Length == 0)
    {
      startLine = 0;
      stopLine = 0;
      return false;
    }

    var first = statements[0];
    var last = statements[^1];
    startLine = first.Start.Line;
    stopLine = last.Stop.Line;
    return true;
  }
  public enum ValidationMode
  {
    Strict,
    Lenient
  }
}
