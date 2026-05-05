using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using AresScript.Environment;
using AresScript.Generated;
using AresScript.Symbols;

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
  private readonly bool _traverseFunctionDeclarationBodies;
  private readonly Stack<(IReadOnlyList<AresScriptParameter> Parameters, AresValueSchema ReturnSchema)> _pendingFunctions = new();
  private readonly AresTypeInferenceInterpreter _typeInference;
  private readonly List<AresFunctionInvocation> _functionInvocations = [];

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
  /// <param name="traverseFunctionDeclarationBodies">Whether to visit function bodies at declaration time.
  /// For summary-ordering this can be disabled so calls inside function bodies are not recorded until runtime emits them.</param>
  public AresValidationInterpreter(
    AresScriptEnvironment aresScriptEnvironment,
    ValidationMode mode = ValidationMode.Strict,
    int? line = null,
    bool traverseFunctionDeclarationBodies = true)
  {
    _environment = aresScriptEnvironment;
    _mode = mode;
    _typeInference = new AresTypeInferenceInterpreter(_environment);
    _line = line;
    _traverseFunctionDeclarationBodies = traverseFunctionDeclarationBodies;
  }

  public IReadOnlyList<AresFunctionInvocation> FunctionInvocations => _functionInvocations;

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
      if(_pendingFunctions.Count > 0)
      {
        foreach(var parameter in _pendingFunctions.Peek().Parameters)
        {
          _environment.AssignVariable(
            parameter.Name,
            DummyValueFactory.CreateDummyValue(parameter.Schema),
            parameter.Schema);
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
            memberContext.lvalue().Start.Line,
            memberContext.lvalue().Start.Column
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
            indexContext.lvalue().Start.Line,
            indexContext.lvalue().Start.Column
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
            indexContext.expression().Start.Line,
            indexContext.expression().Start.Column
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
    var expression = context.expression();
    if(expression is not null)
    {
      await Visit(expression);
    }

    if(_pendingFunctions.Count == 0)
    {
      return;
    }

    var expectedSchema = _pendingFunctions.Peek().ReturnSchema;

    var actual = expression is null
      ? AresSchemaBuilder.Entry(AresDataType.Unit).Build()
      : _typeInference.Visit(expression);
    if(!AresScriptTypeHints.IsCompatibleWithTypeHint(actual, expectedSchema))
    {
      throw new AresInterpreterException(
        $"Function return type mismatch. Expected {expectedSchema.Stringify()}, received {actual.Stringify()}.",
        context.Start.Line,
        context.Start.Column
      );
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

    var conditionExpr = expressions[0];
    var conditionSchema = _typeInference.Visit(conditionExpr);
    if(conditionSchema.Type != AresDataType.Boolean && _mode == ValidationMode.Strict)
    {
      throw new AresInterpreterException(
        "Assert condition must be boolean.",
        conditionExpr.Start.Line,
        conditionExpr.Start.Column
      );
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
    var iterableSchema = _typeInference.Visit(expression);
    if(!IsIterableSchema(iterableSchema) && _mode == ValidationMode.Strict)
    {
      throw new AresInterpreterException(
        $"Value is not iterable: {iterableSchema.Type}.",
        expression.Start.Line,
        expression.Start.Column
      );
    }

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
    var functionNameToken = decl.ID();
    if(functionNameToken is null)
    {
      if(_mode == ValidationMode.Strict)
      {
        throw new AresInterpreterException("Incomplete function declaration.", context.Start.Line, context.Start.Column);
      }
      return;
    }

    var functionId = functionNameToken.GetText();
    var parameters = (decl.parameterList()?.parameter() ?? [])
      .Select(parameter =>
      {
        var parameterName = parameter.ID().GetText();
        var parameterSchema = ResolveTypeHint(parameter.typeHint(), $"parameter '{parameterName}' in function '{functionId}'", parameter.Start);
        return new AresScriptParameter(parameterName, parameterSchema);
      })
      .ToArray();
    var declaredReturnSchema = ResolveTypeHint(decl.typeHint(), $"return type hint in function '{functionId}'", context.Start);
    var block = decl.funcBlock();
    if(block is null)
    {
      if(_mode == ValidationMode.Strict)
      {
        throw new AresInterpreterException("Incomplete function declaration.", context.Start.Line, context.Start.Column);
      }
      return;
    }

    var userFunc = new AresScriptFunction(functionId, parameters, block, declaredReturnSchema);
    try
    {
      _environment.AssignFunction(functionId, userFunc);
    }
    catch(InvalidOperationException e)
    {
      throw new AresInterpreterException(e.Message, functionNameToken.Symbol.Line, functionNameToken.Symbol.Column);
    }
    

    if(_line is not null && _line.Value == context.Start.Line)
    {
      return;
    }

    if(!_traverseFunctionDeclarationBodies)
    {
      return;
    }

    _pendingFunctions.Push((parameters, declaredReturnSchema));

    try
    {
      await Visit(block);

      var unitSchema = AresSchemaBuilder.Entry(AresDataType.Unit).Build();
      if(_mode == ValidationMode.Strict
        && !AresScriptTypeHints.IsCompatibleWithTypeHint(unitSchema, declaredReturnSchema)
        && !AlwaysReturns(block))
      {
        throw new AresInterpreterException(
          $"Function '{functionId}' may complete without returning a value of type {declaredReturnSchema.Stringify()}.",
          functionNameToken.Symbol.Line,
          functionNameToken.Symbol.Column
        );
      }
    }
    finally
    {
      _pendingFunctions.Pop();
    }
  }

  public override async Task VisitLambdaExpr(AresLangParser.LambdaExprContext context)
  {
    var lambdaExpression = context.lambdaExpression();
    var parameterNames = lambdaExpression switch
    {
      AresLangParser.LambdaSingleParamContext singleParam => [singleParam.ID().GetText()],
      AresLangParser.LambdaParamListContext paramList => paramList.ID().Select(id => id.GetText()).ToArray(),
      _ => []
    };

    var body = lambdaExpression switch
    {
      AresLangParser.LambdaSingleParamContext singleParam => singleParam.expression(),
      AresLangParser.LambdaParamListContext paramList => paramList.expression(),
      _ => throw new AresInterpreterException("Invalid lambda expression.", context.Start.Line, context.Start.Column)
    };

    _environment.EnterScope();
    try
    {
      foreach(var parameter in parameterNames)
      {
        _environment.AssignVariable(parameter, new AresValue());
      }

      await Visit(body);
    }
    finally
    {
      _environment.ExitScope();
    }
  }

  public override Task VisitId([NotNull] AresLangParser.IdContext context)
  {
    var id = context.ID().GetText();
    if(_environment.TryGetValue(id, out _)
      || _environment.TryGetSystemFunction(id, out _)
      || _environment.TryGetUserFunction(id, out _)
      || _environment.TryGetUserLambda(id, out _))
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
    var id = context.ID().GetText();
    var receiverSchema = _typeInference.Visit(ctxExpr);
    if(!TryResolveValue(ctxExpr, out var value))
    {
      throw new AresInterpreterException(
        $"Unknown identifier '{ctxExpr.GetText()}'.",
        ctxExpr.Start.Line,
        ctxExpr.Start.Column
      );
    }

    if(value is null)
    {
      throw new AresInterpreterException(
        $"Unable to resolve value of {ctxExpr.GetText()}",
        ctxExpr.Start.Line,
        ctxExpr.Start.Column
      );
    }

    if(receiverSchema.Type is AresDataType.Any or AresDataType.UnspecifiedType)
    {
      return Task.CompletedTask;
    }

    if(value.KindCase == AresValue.KindOneofCase.StructValue)
    {
      if(value.StructValue.Fields.ContainsKey(id))
      {
        return Task.CompletedTask;
      }
    }

    if(value.KindCase == AresValue.KindOneofCase.QuantityValue)
    {
      if(nameof(value.QuantityValue.Scalar).Equals(id, StringComparison.OrdinalIgnoreCase))
      {
        return Task.CompletedTask;
      }
    }

    if(_environment.TryGetExtensionFunction(value, id, out _))
    {
      return Task.CompletedTask;
    }

    throw new AresInterpreterException(
      $"Unknown identifier {context.ID().GetText()} on {ctxExpr.GetText()}.",
      context.Stop.Line,
      context.Stop.Column
    );
  }

  public override async Task VisitIndexAccess([NotNull] AresLangParser.IndexAccessContext context)
  {
    var receiver = context.expression(0);
    var index = context.expression(1);
    await Visit(receiver);
    await Visit(index);

    var receiverSchema = _typeInference.Visit(receiver);
    if(receiverSchema.Type is not AresDataType.Any and not AresDataType.UnspecifiedType
      && !IsIndexableSchema(receiverSchema))
    {
      throw new AresInterpreterException(
        "Cannot access index of a value that is not of list or struct type.",
        receiver.Start.Line,
        receiver.Start.Column
      );
    }

    var indexSchema = _typeInference.Visit(index);
    if(receiverSchema.Type == AresDataType.Struct)
    {
      if(indexSchema.Type is not AresDataType.Any and not AresDataType.UnspecifiedType and not AresDataType.String)
      {
        throw new AresInterpreterException(
          "Provided index expression was not a string.",
          index.Start.Line,
          index.Start.Column
        );
      }

      return;
    }

    if(receiverSchema.Type is not AresDataType.Any and not AresDataType.UnspecifiedType
      && indexSchema.Type is not AresDataType.Any and not AresDataType.UnspecifiedType and not AresDataType.Number)
    {
      throw new AresInterpreterException(
        "Provided index expression was not a number.",
        index.Start.Line,
        index.Start.Column
      );
    }
  }

  public override async Task VisitFunctionCall(AresLangParser.FunctionCallContext ctx)
  {
    var ctxExpr = ctx.expression();
    await Visit(ctxExpr);

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

    if(ctxExpr is AresLangParser.MemberAccessContext memberCtx)
    {
      var memberName = memberCtx.ID().GetText();
      var receiverSchema = _typeInference.Visit(memberCtx.expression());
      if(_environment.TryGetExtensionFunction(receiverSchema.Type, memberName, out var extensionFunc))
      {
        ValidateExtensionFunctionArgs(extensionFunc, memberCtx.expression(), receiverSchema, positionalArgs, keywordArgs, ctx);
        RecordFunctionInvocation(extensionFunc.Id, extensionFunc.Name, ctx, AresFunctionInvocationKind.Extension);
        return;
      }
    }

    var functionId = TryResolveFunctionId(ctxExpr);
    if(functionId is null)
    {
      if(ctxExpr is AresLangParser.MemberAccessContext unresolvedMemberCtx)
      {
        throw new AresInterpreterException(
          $"Unknown function '{unresolvedMemberCtx.ID().GetText()}' on '{unresolvedMemberCtx.expression().GetText()}'.",
          unresolvedMemberCtx.ID().Symbol.Line,
          unresolvedMemberCtx.ID().Symbol.Column
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
        var firstKeywordArg = keywordArgs.Values.First();
        throw new AresInterpreterException(
          $"Runtime function '{functionId}' does not support keyword arguments",
          firstKeywordArg.Start.Line,
          firstKeywordArg.Start.Column
        );
      }

      ValidateSystemFunctionArgs(systemFn, positionalArgs, keywordArgs, ctx);
      RecordFunctionInvocation(systemFn.Id, systemFn.Name, ctx, AresFunctionInvocationKind.System);
      return;
    }

    if(_environment.TryGetUserFunction(functionId, out var userFn))
    {
      if(positionalArgs.Count > userFn.ParameterNames.Count)
      {
        var extraArgument = positionalArgs[userFn.ParameterNames.Count];
        throw new AresInterpreterException(
          $"Function '{functionId}' expected {userFn.ParameterNames.Count} arguments but got {positionalArgs.Count}",
          extraArgument.Start.Line,
          extraArgument.Start.Column
        );
      }

      foreach(var (name, _) in keywordArgs)
      {
        var index = FindParameterIndex(userFn.ParameterNames, name);
        if(index < 0)
        {
          var keywordArgument = keywordArgs[name];
          throw new AresInterpreterException(
            $"Function '{functionId}' got an unexpected keyword argument '{name}'",
            keywordArgument.Start.Line,
            keywordArgument.Start.Column
          );
        }

        if(index < positionalArgs.Count)
        {
          var keywordArgument = keywordArgs[name];
          throw new AresInterpreterException(
            $"Function '{functionId}' got multiple values for argument '{name}'",
            keywordArgument.Start.Line,
            keywordArgument.Start.Column
          );
        }
      }

      for(var i = positionalArgs.Count; i < userFn.ParameterNames.Count; i++)
      {
        var name = userFn.ParameterNames[i];
        if(!keywordArgs.ContainsKey(name))
        {
          throw new AresInterpreterException(
            $"Function '{functionId}' missing required argument '{name}'.",
            ctx.Start.Line,
            ctx.Start.Column
          );
        }
      }

      ValidateUserFunctionTypeHints(functionId, userFn, positionalArgs, keywordArgs, ctx);
      RecordFunctionInvocation(userFn.Name, userFn.Name, ctx, AresFunctionInvocationKind.User);
      return;
    }

    if(_environment.TryGetUserLambda(functionId, out var lambda))
    {
      if(positionalArgs.Count > lambda.Parameters.Count)
      {
        var extraArgument = positionalArgs[lambda.Parameters.Count];
        throw new AresInterpreterException(
          $"Function '{functionId}' expected {lambda.Parameters.Count} arguments but got {positionalArgs.Count}",
          extraArgument.Start.Line,
          extraArgument.Start.Column
        );
      }

      foreach(var (name, _) in keywordArgs)
      {
        var index = FindParameterIndex(lambda.Parameters, name);
        if(index < 0)
        {
          var keywordArgument = keywordArgs[name];
          throw new AresInterpreterException(
            $"Function '{functionId}' got an unexpected keyword argument '{name}'",
            keywordArgument.Start.Line,
            keywordArgument.Start.Column
          );
        }

        if(index < positionalArgs.Count)
        {
          var keywordArgument = keywordArgs[name];
          throw new AresInterpreterException(
            $"Function '{functionId}' got multiple values for argument '{name}'",
            keywordArgument.Start.Line,
            keywordArgument.Start.Column
          );
        }
      }

      for(var i = positionalArgs.Count; i < lambda.Parameters.Count; i++)
      {
        var name = lambda.Parameters[i];
        if(!keywordArgs.ContainsKey(name))
        {
          throw new AresInterpreterException(
            $"Function '{functionId}' missing required argument '{name}'.",
            ctx.Start.Line,
            ctx.Start.Column
          );
        }
      }

      RecordFunctionInvocation(functionId, functionId, ctx, AresFunctionInvocationKind.Lambda);
      return;
    }

    if(_functionDepth == 0)
    {
      throw new AresInterpreterException($"Function '{functionId}' not found.", ctx.Start.Line, ctx.Start.Column);
    }
  }

  public override async Task VisitUnaryMinus([NotNull] AresLangParser.UnaryMinusContext context)
  {
    await Visit(context.expression());
    ValidateArithmeticExpression(context.expression(), $"Cannot perform unary minus on type {_typeInference.Visit(context.expression()).Type}.");
  }

  public override async Task VisitMulDiv(AresLangParser.MulDivContext context)
  {
    await Visit(context.expression(0));
    await Visit(context.expression(1));
    ValidateArithmeticOperands(context.expression(0), context.expression(1), "Left hand side is not numeric or quantity.", "Right hand side is not numeric or compatible quantity.", allowRightNumberForQuantityLeft: true);
  }

  public override async Task VisitSub(AresLangParser.SubContext context)
  {
    await Visit(context.expression(0));
    await Visit(context.expression(1));
    ValidateArithmeticOperands(context.expression(0), context.expression(1), "Left hand side is not numeric or quantity.", "Right hand side is not numeric or compatible quantity.", allowRightNumberForQuantityLeft: false);
  }

  public override async Task VisitAdd(AresLangParser.AddContext context)
  {
    await Visit(context.expression(0));
    await Visit(context.expression(1));

    var leftSchema = _typeInference.Visit(context.expression(0));
    if(leftSchema.Type == AresDataType.Quantity)
    {
      ValidateArithmeticOperands(
        context.expression(0),
        context.expression(1),
        "Left hand side is not numeric or quantity.",
        "Right hand side is not a compatible quantity.",
        allowRightNumberForQuantityLeft: false);
      return;
    }

    if(leftSchema.Type == AresDataType.Number)
    {
      ValidateNumericExpression(context.expression(1), "Right hand side is not numeric.");
    }
  }

  public override async Task VisitRelational([NotNull] AresLangParser.RelationalContext context)
  {
    await Visit(context.expression(0));
    await Visit(context.expression(1));
    ValidateNumericExpression(context.expression(0), "Left hand side is not numeric.");
    ValidateNumericExpression(context.expression(1), "Right hand side is not numeric.");
  }

  public override async Task VisitLogicalNot([NotNull] AresLangParser.LogicalNotContext context)
  {
    await Visit(context.expression());
    ValidateBooleanExpression(context.expression(), $"Cannot perform negation on type {_typeInference.Visit(context.expression()).Type}.");
  }

  public override async Task VisitLogicAnd([NotNull] AresLangParser.LogicAndContext context)
  {
    await Visit(context.expression(0));
    await Visit(context.expression(1));
    ValidateBooleanExpression(context.expression(0), $"Cannot perform AND on type {_typeInference.Visit(context.expression(0)).Type}.");
    ValidateBooleanExpression(context.expression(1), $"Cannot perform AND on type {_typeInference.Visit(context.expression(1)).Type}.");
  }

  public override async Task VisitLogicOr([NotNull] AresLangParser.LogicOrContext context)
  {
    await Visit(context.expression(0));
    await Visit(context.expression(1));
    ValidateBooleanExpression(context.expression(0), $"Cannot perform OR on type {_typeInference.Visit(context.expression(0)).Type}.");
    ValidateBooleanExpression(context.expression(1), $"Cannot perform OR on type {_typeInference.Visit(context.expression(1)).Type}.");
  }

  private void RecordFunctionInvocation(
    string functionId,
    string functionName,
    AresLangParser.FunctionCallContext context,
    AresFunctionInvocationKind kind)
  {
    _functionInvocations.Add(new AresFunctionInvocation(
      functionId,
      functionName,
      context.GetText(),
      context.Start.Line,
      context.Start.Column + 1,
      kind));
  }

  private void ValidateSystemFunctionArgs(AresSystemFunctionSymbol function, IReadOnlyList<AresLangParser.ExpressionContext> positionalArgs, IReadOnlyDictionary<string, AresLangParser.ExpressionContext> keywordArgs, AresLangParser.FunctionCallContext ctx)
  {
    ValidateArgsAgainstSchema(function.Id, function.InputSchema, positionalArgs, keywordArgs, ctx);

    if(function.StaticArgumentValidator is not null)
    {
      var resolvedArgs = ResolveStaticValidatorArgs(function.InputSchema, positionalArgs, keywordArgs);
      var validation = function.StaticArgumentValidator(resolvedArgs);
      if(!validation.Success)
      {
        var arg = ResolveStaticValidatorErrorExpression(function.InputSchema, positionalArgs, keywordArgs, validation.Index);
        throw new AresInterpreterException(
          validation.Error ?? "",
          arg?.Start.Line ?? ctx.Start.Line,
          arg?.Start.Column ?? ctx.Start.Column
        );
      }
    }
  }

  private AresValue?[] ResolveStaticValidatorArgs(
    AresStructSchema schema,
    IReadOnlyList<AresLangParser.ExpressionContext> positionalArgs,
    IReadOnlyDictionary<string, AresLangParser.ExpressionContext> keywordArgs)
  {
    // Runtime/system functions currently reject keyword args before static validators run,
    // so today this mostly preserves positional behavior. Keep the schema-ordered resolution
    // here so validators are ready if runtime keyword-arg support is added later.
    var schemaFields = schema.Fields.ToArray();
    if(schemaFields.Length == 0)
    {
      return positionalArgs.Select(TryBuildAssignmentValue).ToArray();
    }

    var resolvedArgs = new AresValue?[schemaFields.Length];
    for(var i = 0; i < schemaFields.Length; i++)
    {
      AresLangParser.ExpressionContext? argument = null;
      if(i < positionalArgs.Count)
      {
        argument = positionalArgs[i];
      }
      else if(keywordArgs.TryGetValue(schemaFields[i].Key, out var keywordArgument))
      {
        argument = keywordArgument;
      }

      resolvedArgs[i] = argument is null ? null : TryBuildAssignmentValue(argument);
    }

    return resolvedArgs;
  }

  private static AresLangParser.ExpressionContext? ResolveStaticValidatorErrorExpression(
    AresStructSchema schema,
    IReadOnlyList<AresLangParser.ExpressionContext> positionalArgs,
    IReadOnlyDictionary<string, AresLangParser.ExpressionContext> keywordArgs,
    int index)
  {
    if(index < 0)
    {
      return null;
    }

    if(index < positionalArgs.Count)
    {
      return positionalArgs[index];
    }

    var schemaFields = schema.Fields.ToArray();
    if(index < schemaFields.Length && keywordArgs.TryGetValue(schemaFields[index].Key, out var keywordArgument))
    {
      return keywordArgument;
    }

    return null;
  }

  private void ValidateExtensionFunctionArgs(
    AresSystemFunctionSymbol function,
    AresLangParser.ExpressionContext receiverExpression,
    AresValueSchema receiverSchema,
    IReadOnlyList<AresLangParser.ExpressionContext> positionalArgs,
    IReadOnlyDictionary<string, AresLangParser.ExpressionContext> keywordArgs,
    AresLangParser.FunctionCallContext ctx)
  {
    if(keywordArgs.Count > 0)
    {
      var firstKeywordArg = keywordArgs.Values.First();
      throw new AresInterpreterException(
        $"Runtime function '{function.Name}' does not support keyword arguments",
        firstKeywordArg.Start.Line,
        firstKeywordArg.Start.Column
      );
    }

    if(function.InputSchema.Fields.Count == 0)
    {
      return;
    }

    var receiverExpected = function.InputSchema.Fields.First().Value;
    if(!AresScriptTypeHints.IsCompatibleWithTypeHint(receiverSchema, receiverExpected))
    {
      throw new AresInterpreterException(
        $"Function '{function.Id}' receiver type mismatch. Expected {receiverExpected.Stringify()}, received {receiverSchema.Stringify()}.",
        ctx.expression().Start.Line,
        ctx.expression().Start.Column
      );
    }

    var trimmedSchema = TrimReceiverFromSchema(function.InputSchema);
    ValidateArgsAgainstSchema(function.Id, trimmedSchema, positionalArgs, keywordArgs, ctx);

    if(function.StaticArgumentValidator is not null)
    {
      var validatorArgs = new List<AresLangParser.ExpressionContext>(positionalArgs.Count + 1) { receiverExpression };
      validatorArgs.AddRange(positionalArgs);

      var resolvedArgs = ResolveStaticValidatorArgs(function.InputSchema, validatorArgs, keywordArgs);
      if(resolvedArgs.Length > 0 && resolvedArgs[0] is null)
      {
        resolvedArgs[0] = TryBuildAssignmentValue(receiverExpression) ?? DummyValueFactory.CreateDummyValue(receiverSchema);
      }

      var validation = function.StaticArgumentValidator(resolvedArgs);
      if(!validation.Success)
      {
        var arg = validation.Index == 0
          ? receiverExpression
          : ResolveStaticValidatorErrorExpression(function.InputSchema, validatorArgs, keywordArgs, validation.Index);
        throw new AresInterpreterException(
          validation.Error ?? "",
          arg?.Start.Line ?? ctx.Start.Line,
          arg?.Start.Column ?? ctx.Start.Column
        );
      }
    }
  }

  private void ValidateArgsAgainstSchema(
    string functionId,
    AresStructSchema schema,
    IReadOnlyList<AresLangParser.ExpressionContext> positionalArgs,
    IReadOnlyDictionary<string, AresLangParser.ExpressionContext> keywordArgs,
    AresLangParser.FunctionCallContext ctx)
  {
    var schemaFields = schema.Fields.ToArray();
    var variadicAnyArgs = IsVariadicAnyArgsSchema(schemaFields);

    if(schema.Fields.Count == 0 && keywordArgs.Count == 0)
    {
      return;
    }

    foreach(var (name, expr) in keywordArgs)
    {
      if(!schema.Fields.TryGetValue(name, out var expected))
      {
        throw new AresInterpreterException(
          $"Function '{functionId}' got an unexpected keyword argument '{name}'.",
          expr.Start.Line,
          expr.Start.Column
        );
      }

      var actual = _typeInference.Visit(expr);
      if(!AresScriptTypeHints.IsCompatibleWithTypeHint(actual, expected))
      {
        throw new AresInterpreterException(
          $"Function '{functionId}' argument '{name}' type mismatch. Expected {expected.Stringify()}, received {actual.Stringify()}.",
          expr.Start.Line,
          expr.Start.Column
        );
      }
    }

    if(keywordArgs.Count == 0)
    {
      if(!variadicAnyArgs && positionalArgs.Count > schemaFields.Length)
      {
        var extraArgument = positionalArgs[schemaFields.Length];
        throw new AresInterpreterException(
          $"Function '{functionId}' expected at most {schemaFields.Length} arguments but got {positionalArgs.Count}.",
          extraArgument.Start.Line,
          extraArgument.Start.Column
        );
      }

      if(!variadicAnyArgs)
      {
        var requiredCount = schemaFields.Count(field => !field.Value.Optional);
        if(positionalArgs.Count < requiredCount)
        {
          throw new AresInterpreterException(
            $"Function '{functionId}' expected at least {requiredCount} arguments but got {positionalArgs.Count}.",
            ctx.Start.Line,
            ctx.Start.Column
          );
        }
      }

      var positionalTypeChecks = variadicAnyArgs ? 0 : Math.Min(positionalArgs.Count, schemaFields.Length);
      for(var i = 0; i < positionalTypeChecks; i++)
      {
        var (name, expected) = schemaFields[i];
        var actual = _typeInference.Visit(positionalArgs[i]);
        if(!AresScriptTypeHints.IsCompatibleWithTypeHint(actual, expected))
        {
          throw new AresInterpreterException(
            $"Function '{functionId}' argument '{name}' type mismatch. Expected {expected.Stringify()}, received {actual.Stringify()}.",
            positionalArgs[i].Start.Line,
            positionalArgs[i].Start.Column
          );
        }
      }
    }
  }

  private static bool IsVariadicAnyArgsSchema(IReadOnlyList<KeyValuePair<string, AresValueSchema>> schemaFields)
  {
    if(schemaFields.Count != 1)
    {
      return false;
    }

    var (name, entry) = schemaFields[0];
    return string.Equals(name, "args", StringComparison.Ordinal) && entry.Type == AresDataType.Any;
  }

  private static AresStructSchema TrimReceiverFromSchema(AresStructSchema schema)
  {
    if(schema.Fields.Count <= 1)
    {
      return new AresStructSchema();
    }

    var trimmed = new AresStructSchema();
    foreach(var (name, entry) in schema.Fields.Skip(1))
    {
      trimmed.Fields[name] = entry;
    }

    return trimmed;
  }

  private void ValidateUserFunctionTypeHints(
    string functionId,
    AresScriptFunction userFunction,
    IReadOnlyList<AresLangParser.ExpressionContext> positionalArgs,
    IReadOnlyDictionary<string, AresLangParser.ExpressionContext> keywordArgs,
    AresLangParser.FunctionCallContext context)
  {
    for(var i = 0; i < userFunction.Parameters.Count; i++)
    {
      var parameter = userFunction.Parameters[i];
      var parameterName = parameter.Name;
      var expectedSchema = parameter.Schema;

      AresLangParser.ExpressionContext? argument = null;
      if(i < positionalArgs.Count)
      {
        argument = positionalArgs[i];
      }
      else if(keywordArgs.TryGetValue(parameterName, out var keywordArgument))
      {
        argument = keywordArgument;
      }

      if(argument is null)
      {
        continue;
      }

      var actual = _typeInference.Visit(argument);
      if(AresScriptTypeHints.IsCompatibleWithTypeHint(actual, expectedSchema))
      {
        continue;
      }

      throw new AresInterpreterException(
        $"Function '{functionId}' argument '{parameterName}' type mismatch. Expected {expectedSchema.Stringify()}, received {actual.Stringify()}.",
        argument.Start.Line,
        argument.Start.Column
      );
    }
  }

  private AresValueSchema ResolveTypeHint(AresLangParser.TypeHintContext? typeHint, string targetName, IToken token)
  {
    if(AresScriptTypeHints.TryParseTypeHint(typeHint, out var resolvedSchema, out var error))
    {
      return resolvedSchema;
    }

    if(_mode == ValidationMode.Strict)
    {
      throw new AresInterpreterException(
        string.IsNullOrWhiteSpace(error)
          ? $"Unknown type hint '{typeHint?.GetText()}' for {targetName}."
          : $"Invalid type hint '{typeHint?.GetText()}' for {targetName}: {error}",
        token.Line,
        token.Column
      );
    }

    return AresSchemaBuilder.Entry(AresDataType.Any).Build();
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

  private void ValidateArithmeticExpression(AresLangParser.ExpressionContext expression, string message)
  {
    var schema = _typeInference.Visit(expression);
    if(schema.Type is AresDataType.Any or AresDataType.UnspecifiedType or AresDataType.Number or AresDataType.Quantity)
    {
      return;
    }

    if(_mode == ValidationMode.Strict)
    {
      throw new AresInterpreterException(message, expression.Start.Line, expression.Start.Column);
    }
  }

  private void ValidateNumericExpression(AresLangParser.ExpressionContext expression, string message)
  {
    var schema = _typeInference.Visit(expression);
    if(schema.Type is AresDataType.Any or AresDataType.UnspecifiedType or AresDataType.Number)
    {
      return;
    }

    if(_mode == ValidationMode.Strict)
    {
      throw new AresInterpreterException(message, expression.Start.Line, expression.Start.Column);
    }
  }

  private void ValidateArithmeticOperands(
    AresLangParser.ExpressionContext leftExpression,
    AresLangParser.ExpressionContext rightExpression,
    string leftMessage,
    string rightMessage,
    bool allowRightNumberForQuantityLeft)
  {
    var leftSchema = _typeInference.Visit(leftExpression);
    var rightSchema = _typeInference.Visit(rightExpression);

    if(leftSchema.Type is AresDataType.Any or AresDataType.UnspecifiedType)
    {
      return;
    }

    if(leftSchema.Type == AresDataType.Number)
    {
      if(rightSchema.Type is AresDataType.Any or AresDataType.UnspecifiedType or AresDataType.Number)
      {
        return;
      }

      if(_mode == ValidationMode.Strict)
      {
        throw new AresInterpreterException(rightMessage, rightExpression.Start.Line, rightExpression.Start.Column);
      }

      return;
    }

    if(leftSchema.Type != AresDataType.Quantity)
    {
      if(_mode == ValidationMode.Strict)
      {
        throw new AresInterpreterException(leftMessage, leftExpression.Start.Line, leftExpression.Start.Column);
      }

      return;
    }

    if(rightSchema.Type is AresDataType.Any or AresDataType.UnspecifiedType)
    {
      return;
    }

    if(allowRightNumberForQuantityLeft && rightSchema.Type == AresDataType.Number)
    {
      return;
    }

    if(rightSchema.Type == AresDataType.Quantity
      && AreQuantitySchemasCompatible(leftSchema.QuantitySchema, rightSchema.QuantitySchema))
    {
      return;
    }

    if(_mode == ValidationMode.Strict)
    {
      throw new AresInterpreterException(rightMessage, rightExpression.Start.Line, rightExpression.Start.Column);
    }
  }

  private static bool AreQuantitySchemasCompatible(QuantitySchema? left, QuantitySchema? right)
  {
    if(left is null || right is null)
    {
      return true;
    }

    return left.QuantityType == QuantityType.Unspecified
      || right.QuantityType == QuantityType.Unspecified
      || left.QuantityType == right.QuantityType;
  }

  private void ValidateBooleanExpression(AresLangParser.ExpressionContext expression, string message)
  {
    var schema = _typeInference.Visit(expression);
    if(schema.Type is AresDataType.Any or AresDataType.UnspecifiedType or AresDataType.Boolean)
    {
      return;
    }

    if(_mode == ValidationMode.Strict)
    {
      throw new AresInterpreterException(message, expression.Start.Line, expression.Start.Column);
    }
  }

  private static bool IsIterableSchema(AresValueSchema schema)
  {
    return schema.Type is AresDataType.Any
      or AresDataType.UnspecifiedType
      or AresDataType.List
      or AresDataType.StringArray
      or AresDataType.NumberArray
      or AresDataType.ByteArray;
  }

  private static bool IsIndexableSchema(AresValueSchema schema)
  {
    return schema.Type is AresDataType.Any
      or AresDataType.UnspecifiedType
      or AresDataType.Struct
      or AresDataType.List
      or AresDataType.StringArray
      or AresDataType.NumberArray
      or AresDataType.ByteArray;
  }

  private static bool AlwaysReturns(AresLangParser.FuncBlockContext block)
  {
    foreach(var statement in block.statement())
    {
      if(AlwaysReturns(statement))
      {
        return true;
      }
    }

    return false;
  }

  private static bool AlwaysReturns(AresLangParser.StatementContext statement)
  {
    return statement switch
    {
      AresLangParser.FuncControlStmtContext funcControlStmt => funcControlStmt.funcControlStatement() is AresLangParser.ReturnStmtContext,
      AresLangParser.IfStmtContext ifStmt => AlwaysReturns(ifStmt),
      _ => false
    };
  }

  private static bool AlwaysReturns(AresLangParser.IfStmtContext ifStatement)
  {
    var stmt = ifStatement.ifStatement();
    var blocks = stmt.block();
    var expressions = stmt.expression();
    if(blocks.Length != expressions.Length + 1)
    {
      return false;
    }

    foreach(var block in blocks)
    {
      if(!AlwaysReturns(block))
      {
        return false;
      }
    }

    return true;
  }

  private static bool AlwaysReturns(AresLangParser.BlockContext block)
  {
    foreach(var statement in block.statement())
    {
      if(AlwaysReturns(statement))
      {
        return true;
      }
    }

    return false;
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

  private bool TryResolveFunctionCallValue(AresLangParser.FunctionCallContext functionCall, out AresValue? value)
  {
    value = null;

    if(functionCall.expression() is AresLangParser.MemberAccessContext memberAccess)
    {
      var receiverSchema = _typeInference.Visit(memberAccess.expression());
      if(_environment.TryGetExtensionFunction(receiverSchema.Type, memberAccess.ID().GetText(), out var extensionFunction))
      {
        var outputVal = DummyValueFactory.CreateDummyValue(extensionFunction.OutputSchema);

        if(outputVal.QuantityValue is not null)
        {
          var (positionalArgs, keywordArgs) = ExtractFunctionCallArguments(functionCall);
          var validatorArgs = new List<AresLangParser.ExpressionContext>(positionalArgs.Count + 1) { memberAccess.expression() };
          validatorArgs.AddRange(positionalArgs);
          var resolvedArgs = ResolveStaticValidatorArgs(extensionFunction.InputSchema, validatorArgs, keywordArgs);

          var unitArg = resolvedArgs.ElementAtOrDefault(1);
          if(unitArg?.HasStringValue == true)
          {
            outputVal.QuantityValue.Unit = unitArg.StringValue;
          }
        }

        value = outputVal;
        return true;
      }
    }

    var funcId = TryResolveFunctionId(functionCall.expression());
    if(funcId is null)
    {
      return false;
    }

    if(_environment.TryGetValue(funcId, out var aliasValue) && aliasValue.FunctionValue is not null)
    {
      funcId = aliasValue.FunctionValue.FunctionId;
    }

    if(_environment.TryGetSystemFunction(funcId, out var systemFunction))
    {
      var outputVal = DummyValueFactory.CreateDummyValue(systemFunction.OutputSchema);
      if(outputVal.QuantityValue is not null && IsQuantityFromFunction(funcId))
      {
        var (positionalArgs, keywordArgs) = ExtractFunctionCallArguments(functionCall);
        var resolvedArgs = ResolveStaticValidatorArgs(systemFunction.InputSchema, positionalArgs, keywordArgs);
        var unitArg = resolvedArgs.ElementAtOrDefault(1);
        if(unitArg?.HasStringValue == true)
        {
          outputVal.QuantityValue.Unit = unitArg.StringValue;
        }
      }

      value = outputVal;
      return true;
    }

    if(_environment.TryGetUserFunction(funcId, out var userFunction))
    {
      value = userFunction.ReturnSchema.Type is AresDataType.Any or AresDataType.UnspecifiedType
        ? new AresValue()
        : DummyValueFactory.CreateDummyValue(userFunction.ReturnSchema);
      return true;
    }

    if(_environment.TryGetUserLambda(funcId, out var _))
    {
      value = new AresValue();
      return true;
    }

    return false;
  }

  private static bool IsQuantityFromFunction(string functionId)
  {
    return functionId.StartsWith("quantity::", StringComparison.OrdinalIgnoreCase)
      && functionId.EndsWith("::from", StringComparison.OrdinalIgnoreCase);
  }

  private static (
    IReadOnlyList<AresLangParser.ExpressionContext> PositionalArgs,
    IReadOnlyDictionary<string, AresLangParser.ExpressionContext> KeywordArgs)
    ExtractFunctionCallArguments(AresLangParser.FunctionCallContext ctx)
  {
    var positionalArgs = new List<AresLangParser.ExpressionContext>();
    var keywordArgs = new Dictionary<string, AresLangParser.ExpressionContext>(StringComparer.Ordinal);

    var argContexts = ctx.argList()?.argument() ?? Enumerable.Empty<AresLangParser.ArgumentContext>();
    foreach(var argCtx in argContexts)
    {
      switch(argCtx)
      {
        case AresLangParser.PositionalArgContext positionalArg:
          positionalArgs.Add(positionalArg.expression());
          break;
        case AresLangParser.KeywordArgContext keywordArg:
          keywordArgs[keywordArg.ID().GetText()] = keywordArg.expression();
          break;
      }
    }

    return (positionalArgs, keywordArgs);
  }

  private AresValue? TryBuildAssignmentValue(AresLangParser.ExpressionContext expression)
  {
    var functionId = TryResolveFunctionId(expression);
    if(functionId is not null)
    {
      if(_environment.TryGetSystemFunction(functionId, out var _)
        || _environment.TryGetUserFunction(functionId, out var _)
        || _environment.TryGetUserLambda(functionId, out var _))
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
                  var value = TryBuildAssignmentValue(structMember.expression());
                  aresStruct.StructValue.Fields[key] = value ?? AresValueHelper.CreateNull();
                }

                return aresStruct;
              }
            case AresLangParser.IntContext intContext:
              var intText = intContext.INT().GetText().Replace("_", string.Empty, StringComparison.Ordinal);
              if(int.TryParse(intText, out var intValue))
              {
                return AresValueHelper.CreateNumber(intValue);
              }
              break;
            case AresLangParser.FloatContext floatContext:
              var floatText = floatContext.FLOAT().GetText().Replace("_", string.Empty, StringComparison.Ordinal);
              if(double.TryParse(floatText, out var doubleValue))
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
            case AresLangParser.LambdaExprContext lambdaContext:
              return CreateLambdaFunctionValue(lambdaContext.lambdaExpression());
          }
          break;
        }
      case AresLangParser.FunctionCallContext functionCallContext:
        {
          if(TryResolveFunctionCallValue(functionCallContext, out var functionResult))
          {
            return functionResult;
          }
          break;
        }
    }
    // Fallback for computed expressions (math, comparisons, logical ops, etc.)
    // so assigned variables are still introduced with a reasonable placeholder shape.
    var inferredSchema = _typeInference.Visit(expression);
    if(inferredSchema.Type is AresDataType.Any or AresDataType.UnspecifiedType)
    {
      return new AresValue();
    }

    return DummyValueFactory.CreateDummyValue(inferredSchema);
  }

  private AresValue CreateLambdaFunctionValue(AresLangParser.LambdaExpressionContext lambdaExpression)
  {
    var parameters = lambdaExpression switch
    {
      AresLangParser.LambdaSingleParamContext singleParam => [singleParam.ID().GetText()],
      AresLangParser.LambdaParamListContext paramList => paramList.ID().Select(id => id.GetText()).ToArray(),
      _ => []
    };

    var body = lambdaExpression switch
    {
      AresLangParser.LambdaSingleParamContext singleParam => singleParam.expression(),
      AresLangParser.LambdaParamListContext paramList => paramList.expression(),
      _ => throw new AresInterpreterException("Invalid lambda expression.", lambdaExpression.Start.Line, lambdaExpression.Start.Column)
    };

    var closure = _environment.GetAllUserVariableSymbols()
      .ToDictionary(kv => kv.Key, kv => kv.Value.Value.Clone(), StringComparer.Ordinal);
    var lambdaId = $"lambda::{Guid.NewGuid():N}";
    _environment.AssignLambda(lambdaId, new AresScriptLambda(lambdaId, parameters, body, closure));
    return AresValueHelper.CreateFunction(lambdaId);
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
      var hasValue = TryResolveValue(memberAccess.expression(), out var baseValue);

      if(hasValue
        && baseValue?.StructValue is not null
        && baseValue.StructValue.Fields.TryGetValue(memberAccess.ID().GetText(), out var member))
      {
        value = member;
        return true;
      }
      else if(hasValue)
      {
        value = baseValue;
        return true;
      }
    }

    if(expression is AresLangParser.FunctionCallContext functionCall)
    {
      return TryResolveFunctionCallValue(functionCall, out value);
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
