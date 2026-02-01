using Antlr4.Runtime.Misc;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using AresScript.Generated;
using System.Text.RegularExpressions;
using Google.Protobuf;

namespace AresScript.Interpreters;

/// <summary>
/// The main script execution interpreter. It executes the system functions.
/// </summary>
public class AresBaseInterpreter : AresLangBaseVisitor<Task<AresValue>>
{
  protected readonly AresScriptEnvironment Environment;
  private readonly CancellationToken _cancellationToken;
  private int _lvalueResolutionDepth;

  protected override Task<AresValue> DefaultResult => Task.FromResult(AresValueHelper.CreateUnit());

  public AresBaseInterpreter(CancellationToken cancellationToken = default)
    : this(new AresScriptEnvironment(), cancellationToken)
  {
  }

  public AresBaseInterpreter(AresScriptEnvironment aresScriptEnvironment, CancellationToken cancellationToken = default)
  {
    Environment = aresScriptEnvironment ?? throw new ArgumentNullException(nameof(aresScriptEnvironment));
    _cancellationToken = cancellationToken;
  }

  private void ThrowIfCancellationRequested()
  {
    _cancellationToken.ThrowIfCancellationRequested();
  }

  public override async Task<AresValue> VisitProgram(AresLangParser.ProgramContext context)
  {
    ThrowIfCancellationRequested();
    // We must manually iterate and await to ensure statements run one after another.
    foreach(var child in context.children)
    {
      ThrowIfCancellationRequested();
      if(child is AresLangParser.StatementContext stmt)
      {
        await Visit(stmt);
      }
    }
    return AresValueHelper.CreateUnit();
  }

  public override async Task<AresValue> VisitBlock(AresLangParser.BlockContext context)
  {
    ThrowIfCancellationRequested();
    // Iterate over the statements in the block and await them
    foreach(var stmt in context.statement())
    {
      ThrowIfCancellationRequested();
      await Visit(stmt);
    }
    return AresValueHelper.CreateUnit();
  }

  public override async Task<AresValue> VisitFuncBlock(AresLangParser.FuncBlockContext context)
  {
    ThrowIfCancellationRequested();
    // Iterate over the statements in the block and await them
    foreach(var stmt in context.statement())
    {
      ThrowIfCancellationRequested();
      try
      {
        await Visit(stmt);
      }
      catch(ReturnControlFlowException e)
      {
        return e.Value;
      }
    }
    
    return AresValueHelper.CreateUnit();
  }

  public override async Task<AresValue> VisitLoopBlock(AresLangParser.LoopBlockContext context)
  {
    ThrowIfCancellationRequested();
    foreach(var stmt in context.statement())
    {
      ThrowIfCancellationRequested();
      await Visit(stmt);
    }
    return AresValueHelper.CreateUnit();
  }

  public override Task<AresValue> VisitBreakStmt(AresLangParser.BreakStmtContext context)
  {
    throw new LoopBreakException();
  }

  public override Task<AresValue> VisitContinueStmt(AresLangParser.ContinueStmtContext context)
  {
    throw new LoopContinueException();
  }

  public override async Task<AresValue> VisitReturnStmt(AresLangParser.ReturnStmtContext context)
  {
    ThrowIfCancellationRequested();
    var expression = context.expression();
    if(expression is null)
      throw new ReturnControlFlowException(AresValueHelper.CreateUnit());
    
    var val = await Visit(context.expression());
    throw new ReturnControlFlowException(val);
  }

  public override async Task<AresValue> VisitAssertStmt(AresLangParser.AssertStmtContext context)
  {
    ThrowIfCancellationRequested();
    var assertContext = context.assertStatement();
    var condition = await Visit(assertContext.expression(0));
    if(condition.KindCase != AresValue.KindOneofCase.BoolValue)
    {
      throw new AresInterpreterException(
        "Assert condition must be boolean.",
        context.Start.Line,
        context.Start.Column
      );
    }

    if(condition.BoolValue)
    {
      return AresValueHelper.CreateUnit();
    }

    var message = "Assertion failed";
    if(assertContext.expression().Length > 1)
    {
      var messageValue = await Visit(assertContext.expression(1));
      message = $"Assertion failed: {messageValue.Stringify()}";
    }

    throw new AresInterpreterException($"{message}.", context.Start.Line, context.Start.Column);
  }

  public override async Task<AresValue> VisitAssignStmt(AresLangParser.AssignStmtContext context)
  {
    ThrowIfCancellationRequested();
    var assignment = context.assignment();
    if(assignment.lvalue() is AresLangParser.LValueIndexContext indexContext)
    {
      _lvalueResolutionDepth++;
      AresValue container;
      try
      {
        container = await Visit(indexContext.lvalue());
      }
      finally
      {
        _lvalueResolutionDepth--;
      }
      var indexVal = await Visit(indexContext.expression());
      if(container.KindCase == AresValue.KindOneofCase.StructValue)
      {
        if(!indexVal.HasStringValue)
        {
          throw new AresInterpreterException(
            "Provided index expression was not a string.",
            context.Start.Line,
            context.Start.Column
          );
        }

        var newValueForStruct = await Visit(assignment.expression());
        container.StructValue.Fields[indexVal.StringValue] = newValueForStruct;
        return AresValueHelper.CreateUnit();
      }

      if(!indexVal.HasNumberValue)
      {
        throw new AresInterpreterException(
          "Provided index expression was not a number.",
          context.Start.Line,
          context.Start.Column
        );
      }

      var index = Convert.ToInt32(indexVal.NumberValue);
      var newValue = await Visit(assignment.expression());

      switch(container.KindCase)
      {
        case AresValue.KindOneofCase.BytesValue:
        {
          if(!newValue.HasNumberValue)
          {
            throw new AresInterpreterException(
              "Assigned value must be numeric for bytes.",
              context.Start.Line,
              context.Start.Column
            );
          }

          var byteValue = newValue.NumberValue;
          if(byteValue < byte.MinValue || byteValue > byte.MaxValue)
          {
            throw new AresInterpreterException(
              "Assigned byte value is out of range.",
              context.Start.Line,
              context.Start.Column
            );
          }

          var bytes = container.BytesValue.ToByteArray();
          bytes[index] = (byte)byteValue;
          container.BytesValue = ByteString.CopyFrom(bytes);
          return AresValueHelper.CreateUnit();
        }
        case AresValue.KindOneofCase.StringArrayValue:
        {
          if(!newValue.HasStringValue)
          {
            throw new AresInterpreterException(
              "Assigned value must be a string.",
              context.Start.Line,
              context.Start.Column
            );
          }

          container.StringArrayValue.Strings[index] = newValue.StringValue;
          return AresValueHelper.CreateUnit();
        }
        case AresValue.KindOneofCase.NumberArrayValue:
        {
          if(!newValue.HasNumberValue)
          {
            throw new AresInterpreterException(
              "Assigned value must be numeric.",
              context.Start.Line,
              context.Start.Column
            );
          }

          container.NumberArrayValue.Numbers[index] = newValue.NumberValue;
          return AresValueHelper.CreateUnit();
        }
        case AresValue.KindOneofCase.ListValue:
        {
          var listValue = container.ListValue.Values[index];
          listValue.ClearKind();
          listValue.MergeFrom(newValue);
          return AresValueHelper.CreateUnit();
        }
        default:
          throw new AresInterpreterException(
            $"Cannot assign to index of type {container.KindCase}.",
            context.Start.Line,
            context.Start.Column
          );
      }
    }

    var aresVal = await Visit(assignment.lvalue());
    var assignedValue = await Visit(assignment.expression());
    aresVal.ClearKind();
    aresVal.MergeFrom(assignedValue);
    return AresValueHelper.CreateUnit();
  }

  public override async Task<AresValue> VisitExprStmt(AresLangParser.ExprStmtContext context)
  {
    ThrowIfCancellationRequested();
    await Visit(context.expression());
    return AresValueHelper.CreateUnit();
  }

  public override async Task<AresValue> VisitWhileStmt([NotNull] AresLangParser.WhileStmtContext context)
  {
    while(true)
    {
      ThrowIfCancellationRequested();
      var condition = await Visit(context.whileStatement().expression());
      if(!condition.HasBoolValue || !condition.BoolValue)
      {
        break;
      }

      try
      {
        await Visit(context.whileStatement().loopBlock());
      }
      catch(LoopContinueException)
      {
      }
      catch(LoopBreakException)
      {
        break;
      }
    }

    return AresValueHelper.CreateUnit();
  }

  public override async Task<AresValue> VisitForStmt([NotNull] AresLangParser.ForStmtContext context)
  {
    ThrowIfCancellationRequested();
    var iterable = await Visit(context.forStatement().expression());
    var iterVar = context.forStatement().ID().GetText();

    var items = iterable.KindCase switch
    {
      AresValue.KindOneofCase.ListValue => iterable.ListValue.Values,
      AresValue.KindOneofCase.StringArrayValue => iterable.StringArrayValue.Strings.Select(AresValueHelper.CreateString),
      AresValue.KindOneofCase.NumberArrayValue => iterable.NumberArrayValue.Numbers.Select(AresValueHelper.CreateNumber),
      AresValue.KindOneofCase.BytesValue => iterable.BytesValue.Select(b => AresValueHelper.CreateNumber(b)),
      _ => throw new AresInterpreterException(
        $"Value is not iterable: {iterable.KindCase}.",
        context.Start.Line,
        context.Start.Column
      )
    };

    foreach(var item in items)
    {
      ThrowIfCancellationRequested();
      Environment[iterVar] = item;
      try
      {
        await Visit(context.forStatement().loopBlock());
      }
      catch(LoopContinueException)
      {
      }
      catch(LoopBreakException)
      {
        break;
      }
    }

    return AresValueHelper.CreateUnit();
  }

  public override async Task<AresValue> VisitParallelBlock(AresLangParser.ParallelBlockContext context)
  {
    ThrowIfCancellationRequested();
    var expContexts = context.expression();
    var expTasks = expContexts.Select(Visit);
    await Task.WhenAll(expTasks);
    return AresValueHelper.CreateUnit();
  }

  public override Task<AresValue> VisitLValueId(AresLangParser.LValueIdContext context)
  {
    var baseId = context.ID().GetText();
    if(Environment.TryGetValueCurrentScope(baseId, out var value))
    {
      return Task.FromResult(value);
    }

    if(_lvalueResolutionDepth > 0 && Environment.TryGetUserValue(baseId, out var outerValue))
    {
      return Task.FromResult(outerValue);
    }

    Environment[baseId] = AresValueHelper.CreateNull();
    return Task.FromResult(Environment[baseId]);
  }

  public override async Task<AresValue> VisitLValueMember(AresLangParser.LValueMemberContext context)
  {
    _lvalueResolutionDepth++;
    AresValue value;
    try
    {
      value = await Visit(context.lvalue());
    }
    finally
    {
      _lvalueResolutionDepth--;
    }

    if(value.StructValue is null)
    {
      throw new AresInterpreterException(
        $"Expected a struct value, currently {value.KindCase}.",
        context.Start.Line,
        context.Start.Column
      );
    }
    var id = context.ID().GetText();
    if(value.StructValue.Fields.TryGetValue(id, out var member))
    {
      return member;
    }

    var created = AresValueHelper.CreateNull();
    value.StructValue.Fields[id] = created;
    return created;
  }

  public override async Task<AresValue> VisitLValueIndex(AresLangParser.LValueIndexContext context)
  {
    _lvalueResolutionDepth++;
    AresValue currentValue;
    try
    {
      currentValue = await Visit(context.lvalue());
    }
    finally
    {
      _lvalueResolutionDepth--;
    }

    if(currentValue.KindCase != AresValue.KindOneofCase.BytesValue
        && currentValue.KindCase != AresValue.KindOneofCase.StringArrayValue
        && currentValue.KindCase != AresValue.KindOneofCase.NumberArrayValue
        && currentValue.KindCase != AresValue.KindOneofCase.ListValue)
    {
      throw new AresInterpreterException(
        "Cannot access index of a value that is not of list type.",
        context.Start.Line,
        context.Start.Column
      );
    }

    var indexVal = await Visit(context.expression());
    if(!indexVal.HasNumberValue)
    {
      throw new AresInterpreterException(
        "Provided index expression was not a number.",
        context.Start.Line,
        context.Start.Column
      );
    }

    var index = Convert.ToInt32(indexVal.NumberValue);

    var val = currentValue.KindCase switch
    {
      AresValue.KindOneofCase.BytesValue => AresValueHelper.CreateNumber(currentValue.BytesValue[index]),
      AresValue.KindOneofCase.StringArrayValue => AresValueHelper.CreateString(currentValue.StringArrayValue.Strings[index]),
      AresValue.KindOneofCase.NumberArrayValue => AresValueHelper.CreateNumber(currentValue.NumberArrayValue.Numbers[index]),
      AresValue.KindOneofCase.ListValue => currentValue.ListValue.Values[index],
      _ => throw new AresInterpreterException(
        $"Unsupported data type {currentValue.KindCase}.",
        context.Start.Line,
        context.Start.Column
      )
    };

    return val;
  }

  public override async Task<AresValue> VisitIfStmt([NotNull] AresLangParser.IfStmtContext context)
  {
    var numExpressions = context.ifStatement().expression().Length;

    if(numExpressions > 0)
    {
      for(var i = 0; i < numExpressions; i++)
      {
        var condition = await Visit(context.ifStatement().expression(i));
        var isTrue = condition is { HasBoolValue: true, BoolValue: true };
        if(isTrue)
        {
          await Visit(context.ifStatement().block(i));
          return AresValueHelper.CreateUnit();
        }
      }
    }

    if(context.ifStatement().block().Length > numExpressions)
    {
      await Visit(context.ifStatement().block().Last()); // else block
    }

    return AresValueHelper.CreateUnit();
  }

  public override Task<AresValue> VisitFunctionDecl([NotNull] AresLangParser.FunctionDeclContext context)
  {
    var functionId = context.functionDeclaration().ID(0).GetText();
    var paramIds = context.functionDeclaration().ID()[1..].Select(p => p.GetText()).ToArray();
    var block = context.functionDeclaration().funcBlock();

    var userFunc = new AresScriptFunction(functionId, paramIds, block);
    Environment.AssignFunction(functionId, userFunc);

    return Task.FromResult(AresValueHelper.CreateFunction(functionId));
  }

  #region Atoms
  public override Task<AresValue> VisitInt([NotNull] AresLangParser.IntContext context)
  {
    var isInt = int.TryParse(context.INT().GetText(), out var integer);
    if(!isInt)
    {
      throw new AresInterpreterException("Unable to parse to int.", context.Start.Line, context.Start.Column);
    }
    return Task.FromResult(AresValueHelper.CreateNumber(integer));
  }

  public override Task<AresValue> VisitFloat([NotNull] AresLangParser.FloatContext context)
  {
    var isFloat = double.TryParse(context.FLOAT().GetText(), out var doubleBoi);
    if(!isFloat)
    {
      throw new AresInterpreterException("Unable to parse to float.", context.Start.Line, context.Start.Column);
    }

    return Task.FromResult(AresValueHelper.CreateNumber(doubleBoi));
  }

  public override Task<AresValue> VisitString([NotNull] AresLangParser.StringContext context)
  {
    var raw = context.STRING().GetText();
    return Task.FromResult(AresValueHelper.CreateString(InterpreterHelpers.Unquote(raw)));
  }

  public override Task<AresValue> VisitBool([NotNull] AresLangParser.BoolContext context)
  {
    var boolText = context.BOOL().GetText();
    var boolValue = boolText.Equals("true", StringComparison.OrdinalIgnoreCase);
    return Task.FromResult(AresValueHelper.CreateBool(boolValue));
  }

  public override Task<AresValue> VisitNone(AresLangParser.NoneContext context)
  {
    return Task.FromResult(AresValueHelper.CreateNull());
  }

  public override Task<AresValue> VisitId(AresLangParser.IdContext context)
  {
    var id = context.ID().GetText();
    if(Environment.TryGetSystemFunction(id, out var _) || Environment.TryGetUserFunction(id, out var _))
    {
      return Task.FromResult(AresValueHelper.CreateFunction(id));
    }

    return Task.FromResult(Environment[context.ID().GetText()]);
  }

  public override async Task<AresValue> VisitParens([NotNull] AresLangParser.ParensContext context)
  {
    var exprValue = await Visit(context.expression());

    return exprValue;
  }

  public override async Task<AresValue> VisitArray([NotNull] AresLangParser.ArrayContext context)
  {
    if(context.expression().Length == 0)
    {
      return AresValueHelper.CreateList();
    }

    var aresVals = new List<AresValue>();
    foreach(var expr in context.expression())
    {
      var val = await Visit(expr);
      aresVals.Add(val);
    }

    var initialType = aresVals.First().KindCase;
    var sameType = aresVals.All(v => v.KindCase == initialType);
    if(sameType && initialType == AresValue.KindOneofCase.NumberValue)
    {
      var nums = aresVals.Select(v => v.NumberValue).ToArray();
      return AresValueHelper.CreateNumberArray(nums);
    }

    if(sameType && initialType == AresValue.KindOneofCase.StringValue)
    {
      var strings = aresVals.Select(v => v.StringValue).ToArray();
      return AresValueHelper.CreateStringArray(strings);
    }

    return AresValueHelper.CreateList(aresVals);
  }

  public override async Task<AresValue> VisitStruct([NotNull] AresLangParser.StructContext context)
  {
    var aresStruct = new AresStruct();
    foreach(var pair in context.structure().pair())
    {
      var key = pair.ID()?.GetText() ?? InterpreterHelpers.Unquote(pair.STRING().GetText());
      var value = await Visit(pair.expression());
      aresStruct.AddValue(key, value);
    }

    return AresValueHelper.CreateStruct(aresStruct);
  }
  #endregion

  #region Expressions

  public override async Task<AresValue> VisitMemberAccess([NotNull] AresLangParser.MemberAccessContext context)
  {
    var structVal = await Visit(context.expression());
    if(structVal.KindCase != AresValue.KindOneofCase.StructValue)
    {
      throw new AresInterpreterException(
        $"Trying to access a member of a value that is not a struct. Value type: {structVal.KindCase}.",
        context.Start.Line,
        context.Start.Column
      );
    }

    if(structVal.StructValue.Fields.TryGetValue(context.ID().GetText(), out var aresValue))
    {
      return aresValue;
    }

    return AresValueHelper.CreateNull();
  }

  public override async Task<AresValue> VisitIndexAccess([NotNull] AresLangParser.IndexAccessContext context)
  {
    var currentValue = await Visit(context.expression(0));
    if(currentValue.KindCase != AresValue.KindOneofCase.BytesValue
        && currentValue.KindCase != AresValue.KindOneofCase.StringArrayValue
        && currentValue.KindCase != AresValue.KindOneofCase.NumberArrayValue
        && currentValue.KindCase != AresValue.KindOneofCase.ListValue
        && currentValue.KindCase != AresValue.KindOneofCase.StructValue)
    {
      throw new AresInterpreterException(
        "Cannot access index of a value that is not of list or struct type.",
        context.Start.Line,
        context.Start.Column
      );
    }

    var indexVal = await Visit(context.expression(1));
    if(currentValue.KindCase == AresValue.KindOneofCase.StructValue)
    {
      if(!indexVal.HasStringValue)
      {
        throw new AresInterpreterException(
          "Provided index expression was not a string.",
          context.Start.Line,
          context.Start.Column
        );
      }

      return currentValue.StructValue.Fields.TryGetValue(indexVal.StringValue, out var fieldValue)
        ? fieldValue
        : AresValueHelper.CreateNull();
    }

    if(!indexVal.HasNumberValue)
    {
      throw new AresInterpreterException(
        "Provided index expression was not a number.",
        context.Start.Line,
        context.Start.Column
      );
    }

    var index = Convert.ToInt32(indexVal.NumberValue);
    try
    {
      var val = currentValue.KindCase switch
      {
        AresValue.KindOneofCase.BytesValue => AresValueHelper.CreateNumber(currentValue.BytesValue[index]),
        AresValue.KindOneofCase.StringArrayValue => AresValueHelper.CreateString(currentValue.StringArrayValue.Strings[index]),
        AresValue.KindOneofCase.NumberArrayValue => AresValueHelper.CreateNumber(currentValue.NumberArrayValue.Numbers[index]),
        AresValue.KindOneofCase.ListValue => currentValue.ListValue.Values[index],
        _ => throw new AresInterpreterException(
          $"Unsupported data type {currentValue.KindCase}.",
          context.Start.Line,
          context.Start.Column
        )
      };

      return val;
    }
    catch(ArgumentOutOfRangeException)
    {
      throw new AresInterpreterException("Index was out of range.", context.Start.Line, context.Start.Column);
    }
  }

  public override async Task<AresValue> VisitFunctionCall(AresLangParser.FunctionCallContext ctx)
  {
    ThrowIfCancellationRequested();
    var callee = await Visit(ctx.expression());

    if(callee.FunctionValue is null)
      throw new AresInterpreterException("Attempted to call a non-function");

    var id = callee.FunctionValue.FunctionId;

    var positionalArgs = new List<AresValue>();
    var keywordArgs = new Dictionary<string, AresValue>(StringComparer.Ordinal);
    var seenKeywordArg = false;

    var argContexts = ctx.argList()?.argument() ?? Enumerable.Empty<AresLangParser.ArgumentContext>();
    foreach(var argCtx in argContexts)
    {
      ThrowIfCancellationRequested();
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

          positionalArgs.Add(await Visit(positionalArg.expression()));
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

          keywordArgs[name] = await Visit(keywordArg.expression());
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

    if(Environment.TryGetSystemFunction(id, out var aresFn))
    {
      if(keywordArgs.Count > 0)
      {
        throw new AresInterpreterException($"Runtime function '{id}' does not support keyword arguments");
      }

      ThrowIfCancellationRequested();
      return await aresFn.Body(positionalArgs, _cancellationToken);
    }

    if(!Environment.TryGetUserFunction(id, out var userFn))
      throw new AresInterpreterException($"Function '{id}' not found");
    
    ThrowIfCancellationRequested();
    if(Environment.Depth > 100)
    {
      throw new AresInterpreterException("Maximum function call depth reached.");
    }
    Environment.EnterScope();
    try
    {
      if(positionalArgs.Count > userFn.Parameters.Count)
      {
        throw new AresInterpreterException(
          $"Function '{id}' expected {userFn.Parameters.Count} arguments but got {positionalArgs.Count}"
        );
      }

      for(var i = 0; i < positionalArgs.Count; i++)
      {
        Environment[userFn.Parameters[i]] = positionalArgs[i];
      }

      foreach(var (name, value) in keywordArgs)
      {
        var index = FindParameterIndex(userFn.Parameters, name);
        if(index < 0)
        {
          throw new AresInterpreterException($"Function '{id}' got an unexpected keyword argument '{name}'");
        }

        if(index < positionalArgs.Count)
        {
          throw new AresInterpreterException($"Function '{id}' got multiple values for argument '{name}'");
        }

        Environment[name] = value;
      }

      for(var i = positionalArgs.Count; i < userFn.Parameters.Count; i++)
      {
        var name = userFn.Parameters[i];
        if(!Environment.TryGetValueCurrentScope(name, out var _))
        {
          throw new AresInterpreterException($"Function '{id}' missing required argument '{name}'");
        }
      }

      var result = await Visit(userFn.Body);

      return result;

      static int FindParameterIndex(IReadOnlyList<string> parameters, string name)
      {
        for(var i = 0; i < parameters.Count; i++)
        {
          if(string.Equals(parameters[i], name, StringComparison.Ordinal))
            return i;
        }

        return -1;
      }
    }
    finally
    {
      Environment.ExitScope();
    }
    
  }

  public override async Task<AresValue> VisitUnaryMinus([NotNull] AresLangParser.UnaryMinusContext context)
  {
    var value = await Visit(context.expression());
    if(value.KindCase == AresValue.KindOneofCase.NumberValue)
    {
      return AresValueHelper.CreateNumber(-value.NumberValue);
    }

    throw new AresInterpreterException(
      $"Cannot perform unary minus on type {value.KindCase}.",
      context.Start.Line,
      context.Start.Column
    );
  }

  public override async Task<AresValue> VisitMulDiv(AresLangParser.MulDivContext context)
  {
    var left = await Visit(context.expression(0));
    var right = await Visit(context.expression(1));

    if(!left.HasNumberValue)
    {
      throw new AresInterpreterException("Left hand side is not numeric.", context.Start.Line, context.Start.Column);
    }

    if(!right.HasNumberValue)
    {
      throw new AresInterpreterException("Right hand side is not numeric.", context.Start.Line, context.Start.Column);
    }

    var result = context.op.Type switch
    {
      AresLangParser.MUL => left.NumberValue * right.NumberValue,
      AresLangParser.DIV => left.NumberValue / right.NumberValue,
      AresLangParser.MOD => left.NumberValue % right.NumberValue,
      _ => throw new AresInterpreterException(
        $"Wrong operation type {context.op.Type}.",
        context.op.Line,
        context.op.Column
      )
    };

    return AresValueHelper.CreateNumber(result);
  }

  public override async Task<AresValue> VisitAdd(AresLangParser.AddContext context)
  {
    var left = await Visit(context.expression(0));
    var right = await Visit(context.expression(1));

    if(left.HasNumberValue && right.HasNumberValue)
    {
      return AresValueHelper.CreateNumber(left.NumberValue + right.NumberValue);
    }

    var leftStr = left.Stringify();
    var rightStr = right.Stringify();
    
    return AresValueHelper.CreateString(leftStr + rightStr);
  }

  public override async Task<AresValue> VisitSub(AresLangParser.SubContext context)
  {
    var left = await Visit(context.expression(0));
    var right = await Visit(context.expression(1));
    
    if(!left.HasNumberValue)
    {
      throw new AresInterpreterException("Left hand side is not numeric.", context.Start.Line, context.Start.Column);
    }

    if(!right.HasNumberValue)
    {
      throw new AresInterpreterException("Right hand side is not numeric.", context.Start.Line, context.Start.Column);
    }
    
    return AresValueHelper.CreateNumber(left.NumberValue - right.NumberValue);
  }

  public override async Task<AresValue> VisitRelational([NotNull] AresLangParser.RelationalContext context)
  {
    var left = await Visit(context.expression(0));
    var right = await Visit(context.expression(1));

    if(!left.HasNumberValue)
    {
      throw new AresInterpreterException("Left hand side is not numeric.", context.Start.Line, context.Start.Column);
    }

    if(!right.HasNumberValue)
    {
      throw new AresInterpreterException("Right hand side is not numeric.", context.Start.Line, context.Start.Column);
    }

    var result = context.op.Type switch
    {
      AresLangParser.GT => left.NumberValue > right.NumberValue,
      AresLangParser.LT => left.NumberValue < right.NumberValue,
      AresLangParser.GE => left.NumberValue >= right.NumberValue,
      AresLangParser.LE => left.NumberValue <= right.NumberValue,
      _ => throw new AresInterpreterException(
        $"Wrong operation type {context.op.Type}.",
        context.op.Line,
        context.op.Column
      )
    };

    return AresValueHelper.CreateBool(result);
  }

  public override async Task<AresValue> VisitEquality([NotNull] AresLangParser.EqualityContext context)
  {
    var left = await Visit(context.expression(0));
    var right = await Visit(context.expression(1));

    var result = context.op.Type switch
    {
      AresLangParser.EQ => left.Equals(right),
      AresLangParser.NEQ => !left.Equals(right),
      _ => throw new AresInterpreterException(
        $"Wrong operation type {context.op.Type}.",
        context.op.Line,
        context.op.Column
      )
    };

    return AresValueHelper.CreateBool(result);
  }

  public override async Task<AresValue> VisitLogicalNot([NotNull] AresLangParser.LogicalNotContext context)
  {
    var value = await Visit(context.expression());
    if(value.KindCase == AresValue.KindOneofCase.BoolValue)
    {
      return AresValueHelper.CreateBool(!value.BoolValue);
    }

    throw new AresInterpreterException(
      $"Cannot perform negation on type {value.KindCase}.",
      context.Start.Line,
      context.Start.Column
    );
  }

  public override async Task<AresValue> VisitLogicAnd([NotNull] AresLangParser.LogicAndContext context)
  {
    var left = await Visit(context.expression(0));
    var right = await Visit(context.expression(1));
    if(left.KindCase != AresValue.KindOneofCase.BoolValue)
    {
      throw new AresInterpreterException(
        $"Cannot perform AND on type {left.KindCase}.",
        context.Start.Line,
        context.Start.Column
      );
    }

    if(right.KindCase != AresValue.KindOneofCase.BoolValue)
    {
      throw new AresInterpreterException(
        $"Cannot perform AND on type {right.KindCase}.",
        context.Start.Line,
        context.Start.Column
      );
    }

    return AresValueHelper.CreateBool(left.BoolValue && right.BoolValue);
  }

  public override async Task<AresValue> VisitLogicOr([NotNull] AresLangParser.LogicOrContext context)
  {
    var left = await Visit(context.expression(0));
    var right = await Visit(context.expression(1));
    if(left.KindCase != AresValue.KindOneofCase.BoolValue)
    {
      throw new AresInterpreterException(
        $"Cannot perform OR on type {left.KindCase}.",
        context.Start.Line,
        context.Start.Column
      );
    }

    if(right.KindCase != AresValue.KindOneofCase.BoolValue)
    {
      throw new AresInterpreterException(
        $"Cannot perform OR on type {right.KindCase}.",
        context.Start.Line,
        context.Start.Column
      );
    }

    return AresValueHelper.CreateBool(left.BoolValue || right.BoolValue);
  }

  #endregion
}
