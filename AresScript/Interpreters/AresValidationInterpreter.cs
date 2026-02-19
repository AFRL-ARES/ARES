using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
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
  private readonly bool _traverseFunctionDeclarationBodies;
  private readonly Stack<(IReadOnlyList<AresScriptParameter> Parameters, AresDataType ReturnType)> _pendingFunctions = new();
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
          _environment.AssignVariable(parameter.Name, CreateUnknownValue());
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
    var expression = context.expression();
    if(expression is not null)
    {
      await Visit(expression);
    }

    if(_pendingFunctions.Count == 0)
    {
      return;
    }

    var expectedType = _pendingFunctions.Peek().ReturnType;

    var actual = expression is null
      ? AresSchemaBuilder.Entry(AresDataType.Unit).Build()
      : _typeInference.Visit(expression);
    var expected = AresSchemaBuilder.Entry(expectedType).Build();
    if(!IsCompatible(expected, actual))
    {
      throw new AresInterpreterException(
        $"Function return type mismatch. Expected {expectedType}, received {actual.Type}.",
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
        var parameterType = ResolveTypeHint(parameter.typeHint(), $"parameter '{parameterName}' in function '{functionId}'", parameter.Start);
        return new AresScriptParameter(parameterName, parameterType);
      })
      .ToArray();
    var declaredReturnType = ResolveTypeHint(decl.typeHint(), $"return type hint in function '{functionId}'", context.Start);
    var block = decl.funcBlock();
    if(block is null)
    {
      if(_mode == ValidationMode.Strict)
      {
        throw new AresInterpreterException("Incomplete function declaration.", context.Start.Line, context.Start.Column);
      }
      return;
    }

    var userFunc = new AresScriptFunction(functionId, parameters, block, declaredReturnType);
    _environment.AssignFunction(functionId, userFunc);

    if(_line is not null && _line.Value == context.Start.Line)
    {
      return;
    }

    if(!_traverseFunctionDeclarationBodies)
    {
      return;
    }

    _pendingFunctions.Push((parameters, declaredReturnType));
    
    try
    {
      await Visit(block);
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
        _environment.AssignVariable(parameter, CreateUnknownValue());
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
        context.Start.Line,
        context.Start.Column + 1
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

    if(_environment.TryGetExtensionFunction(value, id, out _))
    {
      return Task.CompletedTask;
    }

    throw new AresInterpreterException(
      $"Unknown identifier {context.ID().GetText()} on {ctxExpr.GetText()}.",
      context.Start.Line,
      context.Stop.Column + 1
    );
  }

  private static AresValue CreateUnknownValue()
  {
    // A value with no active oneof kind maps to UnspecifiedType in schema inference.
    return new AresValue();
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
        ValidateExtensionFunctionArgs(extensionFunc, receiverSchema, positionalArgs, keywordArgs, ctx);
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
          unresolvedMemberCtx.Start.Line,
          unresolvedMemberCtx.Stop.Column + 1
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
      RecordFunctionInvocation(systemFn.Id, systemFn.Name, ctx, AresFunctionInvocationKind.System);
      return;
    }

    if(_environment.TryGetUserFunction(functionId, out var userFn))
    {
      if(positionalArgs.Count > userFn.ParameterNames.Count)
      {
        throw new AresInterpreterException(
          $"Function '{functionId}' expected {userFn.ParameterNames.Count} arguments but got {positionalArgs.Count}"
        );
      }

      foreach(var (name, _) in keywordArgs)
      {
        var index = FindParameterIndex(userFn.ParameterNames, name);
        if(index < 0)
        {
          throw new AresInterpreterException($"Function '{functionId}' got an unexpected keyword argument '{name}'");
        }

        if(index < positionalArgs.Count)
        {
          throw new AresInterpreterException($"Function '{functionId}' got multiple values for argument '{name}'");
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
        throw new AresInterpreterException(
          $"Function '{functionId}' expected {lambda.Parameters.Count} arguments but got {positionalArgs.Count}",
          ctx.argList().Start.Line,
          ctx.argList().Start.Column
        );
      }

      foreach(var (name, _) in keywordArgs)
      {
        var index = FindParameterIndex(lambda.Parameters, name);
        if(index < 0)
        {
          throw new AresInterpreterException(
            $"Function '{functionId}' got an unexpected keyword argument '{name}'",
            ctx.argList().Start.Line,
            ctx.argList().Start.Column
          );
        }

        if(index < positionalArgs.Count)
        {
          throw new AresInterpreterException(
            $"Function '{functionId}' got multiple values for argument '{name}'",
            ctx.argList().Start.Line,
            ctx.argList().Start.Column
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

  private void ValidateSystemFunctionArgs(AresSystemFunction function, IReadOnlyList<AresLangParser.ExpressionContext> positionalArgs, IReadOnlyDictionary<string, AresLangParser.ExpressionContext> keywordArgs, AresLangParser.FunctionCallContext ctx)
  {
    ValidateArgsAgainstSchema(function.Id, function.InputSchema, positionalArgs, keywordArgs, ctx);
  }

  private void ValidateExtensionFunctionArgs(
    AresSystemFunction function,
    SchemaEntry receiverSchema,
    IReadOnlyList<AresLangParser.ExpressionContext> positionalArgs,
    IReadOnlyDictionary<string, AresLangParser.ExpressionContext> keywordArgs,
    AresLangParser.FunctionCallContext ctx)
  {
    if(keywordArgs.Count > 0)
    {
      throw new AresInterpreterException($"Runtime function '{function.Name}' does not support keyword arguments");
    }

    if(function.InputSchema.Fields.Count == 0)
    {
      return;
    }

    var receiverExpected = function.InputSchema.Fields.First().Value;
    if(!IsCompatible(receiverExpected, receiverSchema))
    {
      throw new AresInterpreterException(
        $"Function '{function.Id}' receiver type mismatch. Expected {receiverExpected.Type}, received {receiverSchema.Type}.",
        ctx.Start.Line,
        ctx.Start.Column
      );
    }

    var trimmedSchema = TrimReceiverFromSchema(function.InputSchema);
    ValidateArgsAgainstSchema(function.Id, trimmedSchema, positionalArgs, keywordArgs, ctx);
  }

  private void ValidateArgsAgainstSchema(
    string functionId,
    AresDataSchema schema,
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
          ctx.Start.Line,
          ctx.Start.Column
        );
      }

      var actual = _typeInference.Visit(expr);
      if(!IsCompatible(expected, actual))
      {
        throw new AresInterpreterException(
          $"Function '{functionId}' argument '{name}' type mismatch. Expected {expected.Type}, received {actual.Type}.",
          ctx.Start.Line,
          ctx.Start.Column
        );
      }
    }

    if(keywordArgs.Count == 0)
    {
      if(!variadicAnyArgs && positionalArgs.Count > schemaFields.Length)
      {
        throw new AresInterpreterException(
          $"Function '{functionId}' expected at most {schemaFields.Length} arguments but got {positionalArgs.Count}.",
          ctx.Start.Line,
          ctx.Start.Column
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
        if(!IsCompatible(expected, actual))
        {
          throw new AresInterpreterException(
            $"Function '{functionId}' argument '{name}' type mismatch. Expected {expected.Type}, received {actual.Type}.",
            ctx.Start.Line,
            ctx.Start.Column
          );
        }
      }
    }
  }

  private static bool IsVariadicAnyArgsSchema(IReadOnlyList<KeyValuePair<string, SchemaEntry>> schemaFields)
  {
    if(schemaFields.Count != 1)
    {
      return false;
    }

    var (name, entry) = schemaFields[0];
    return string.Equals(name, "args", StringComparison.Ordinal) && entry.Type == AresDataType.Any;
  }

  private static AresDataSchema TrimReceiverFromSchema(AresDataSchema schema)
  {
    if(schema.Fields.Count <= 1)
    {
      return new AresDataSchema();
    }

    var trimmed = new AresDataSchema();
    foreach(var (name, entry) in schema.Fields.Skip(1))
    {
      trimmed.Fields[name] = entry;
    }

    return trimmed;
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
      var expectedType = parameter.Type;

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

      var expected = AresSchemaBuilder.Entry(expectedType).Build();
      var actual = _typeInference.Visit(argument);
      if(IsCompatible(expected, actual))
      {
        continue;
      }

      throw new AresInterpreterException(
        $"Function '{functionId}' argument '{parameterName}' type mismatch. Expected {expectedType}, received {actual.Type}.",
        context.Start.Line,
        context.Start.Column
      );
    }
  }

  private AresDataType ResolveTypeHint(AresLangParser.TypeHintContext? typeHint, string targetName, IToken token)
  {
    if(typeHint is null)
    {
      return AresDataType.Any;
    }

    var rawTypeHint = typeHint.GetText();
    if(string.IsNullOrWhiteSpace(rawTypeHint))
    {
      return AresDataType.Any;
    }

    if(AresScriptTypeHints.TryParseTypeHint(rawTypeHint, out var resolvedType))
    {
      return resolvedType;
    }

    if(_mode == ValidationMode.Strict)
    {
      throw new AresInterpreterException(
        $"Unknown type hint '{rawTypeHint}' for {targetName}.",
        token.Line,
        token.Column
      );
    }

    return AresDataType.Any;
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
                  aresStruct.StructValue.Fields[key] = AresValueHelper.CreateNull();
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
          var funcId = TryResolveFunctionId(functionCallContext.expression());
          if(funcId is null)
          {
            break;
          }

          if(_environment.TryGetValue(funcId, out var aliasValue) && aliasValue.FunctionValue is not null)
          {
            funcId = aliasValue.FunctionValue.FunctionId;
          }

          if(_environment.TryGetSystemFunction(funcId, out var systemFunction))
          {
            var schema = systemFunction.OutputSchema;
            var dummyValue = InterpreterHelpers.CreateDummyValue(schema);
            return dummyValue;
          }

          if(_environment.TryGetUserFunction(funcId, out var _) || _environment.TryGetUserLambda(funcId, out var _))
          {
            // We cannot know user-function return shape statically in this pass, but assignment target
            // should still be introduced into scope for subsequent validation.
            return CreateUnknownValue();
          }
          break;
        }
    }

    return null;
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
      _ => throw new AresInterpreterException("Invalid lambda expression.")
    };

    var closure = _environment.GetAllUserVariables()
      .ToDictionary(kv => kv.Key, kv => kv.Value.Clone(), StringComparer.Ordinal);
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
