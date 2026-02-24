using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using AresScript.Generated;

namespace AresScript.Interpreters;

/// <summary>
/// Interpreter specifically to gather the types of symbols so they can be displayed via hover
/// or otherwise validated when used as inputs/outputs
/// </summary>
public sealed class AresTypeInferenceInterpreter : AresLangBaseVisitor<AresValueSchema>
{
  private readonly AresScriptEnvironment _environment;

  public AresTypeInferenceInterpreter(AresScriptEnvironment environment)
  {
    _environment = environment;
  }

  protected override AresValueSchema DefaultResult => AresSchemaBuilder.Entry(AresDataType.Any).Build();

  public override AresValueSchema VisitInt(AresLangParser.IntContext context) => AresSchemaBuilder.Entry(AresDataType.Number).Build();
  public override AresValueSchema VisitFloat(AresLangParser.FloatContext context) => AresSchemaBuilder.Entry(AresDataType.Number).Build();
  public override AresValueSchema VisitString(AresLangParser.StringContext context) => AresSchemaBuilder.Entry(AresDataType.String).Build();
  public override AresValueSchema VisitBool(AresLangParser.BoolContext context) => AresSchemaBuilder.Entry(AresDataType.Boolean).Build();
  public override AresValueSchema VisitNone(AresLangParser.NoneContext context) => AresSchemaBuilder.Entry(AresDataType.Null).Build();

  public override AresValueSchema VisitId(AresLangParser.IdContext context)
  {
    var id = context.ID().GetText();
    if(_environment.TryGetSystemFunction(id, out var sysFunc) && sysFunc?.OutputSchema is not null) 
    {
      return sysFunc.OutputSchema;
    }
    if(_environment.TryGetUserFunction(id, out var _))
    {
      return AresSchemaBuilder.Entry(AresDataType.Any).Build();
    }
    if(_environment.TryGetUserLambda(id, out var _))
    {
      return AresSchemaBuilder.Entry(AresDataType.Function).Build();
    }

    if(_environment.TryGetValue(id, out var envVal))
    {
      return envVal.ToAresValueSchema();
    }

    return AresSchemaBuilder.Entry(AresDataType.Any).Build();
  }

  public override AresValueSchema VisitParens(AresLangParser.ParensContext context)
  {
    return Visit(context.expression());
  }

  public override AresValueSchema VisitArray(AresLangParser.ArrayContext context)
  {
    var expressions = context.expression();
    if(expressions.Length == 0)
    {
      return CreateListEntry(AresSchemaBuilder.Entry(AresDataType.Any).Build());
    }

    var elementTypes = expressions.Select(Visit).ToArray();
    var firstType = elementTypes[0].Type;
    var allSame = elementTypes.All(t => t.Type == firstType);

    if(allSame)
    {
      if(firstType == AresDataType.String)
      {
        return AresSchemaBuilder.Entry(AresDataType.StringArray).Build();
      }

      if(firstType == AresDataType.Number)
      {
        return AresSchemaBuilder.Entry(AresDataType.NumberArray).Build();
      }
    }

    return CreateListEntry(AresSchemaBuilder.Entry(AresDataType.Any).Build());
  }

  public override AresValueSchema VisitLambdaExpr(AresLangParser.LambdaExprContext context)
  {
    return AresSchemaBuilder.Entry(AresDataType.Function).Build();
  }

  public override AresValueSchema VisitStruct(AresLangParser.StructContext context)
  {
    var schema = new AresStructSchema();
    foreach(var pair in context.structure().pair())
    {
      var key = pair.ID()?.GetText() ?? Unquote(pair.STRING().GetText());
      var value = Visit(pair.expression());
      schema.Fields[key] = value;
    }

    return CreateStructEntry(schema);
  }

  public override AresValueSchema VisitMemberAccess(AresLangParser.MemberAccessContext context)
  {
    var left = Visit(context.expression());
    if(left.Type == AresDataType.Struct && left.StructSchema is not null)
    {
      var field = context.ID().GetText();
      if(left.StructSchema.Fields.TryGetValue(field, out var entry))
      {
        return entry;
      }
    }

    return AresSchemaBuilder.Entry(AresDataType.Any).Build();
  }

  public override AresValueSchema VisitIndexAccess(AresLangParser.IndexAccessContext context)
  {
    var container = Visit(context.expression(0));
    if(container.Type == AresDataType.Struct && container.StructSchema is not null)
    {
      var indexExpr = context.expression(1);
      if(indexExpr is AresLangParser.AtomExprContext atomExpr && atomExpr.atom() is AresLangParser.StringContext stringCtx)
      {
        var key = Unquote(stringCtx.STRING().GetText());
        if(container.StructSchema.Fields.TryGetValue(key, out var entry))
        {
          return entry;
        }
      }
    }

    return container.Type switch
    {
      AresDataType.StringArray => AresSchemaBuilder.Entry(AresDataType.String).Build(),
      AresDataType.NumberArray => AresSchemaBuilder.Entry(AresDataType.Number).Build(),
      AresDataType.ByteArray => AresSchemaBuilder.Entry(AresDataType.Number).Build(),
      AresDataType.List when container.ListElementSchema is not null => container.ListElementSchema,
      _ => AresSchemaBuilder.Entry(AresDataType.Any).Build()
    };
  }

  public override AresValueSchema VisitFunctionCall(AresLangParser.FunctionCallContext context)
  {
    if(context.expression() is AresLangParser.MemberAccessContext memberAccess)
    {
      var receiverSchema = Visit(memberAccess.expression());
      if(_environment.TryGetExtensionFunction(receiverSchema.Type, memberAccess.ID().GetText(), out var extensionFunc))
      {
        return extensionFunc.OutputSchema;
      }
    }

    var functionId = TryResolveFunctionId(context.expression());
    if(functionId is not null && _environment.TryGetSystemFunction(functionId, out var systemFunc))
    {
      return systemFunc.OutputSchema;
    }
    if(functionId is not null && _environment.TryGetUserLambda(functionId, out var _))
    {
      return AresSchemaBuilder.Entry(AresDataType.Any).Build();
    }

    return AresSchemaBuilder.Entry(AresDataType.Any).Build();
  }

  public override AresValueSchema VisitUnaryMinus(AresLangParser.UnaryMinusContext context)
  {
    return AresSchemaBuilder.Entry(AresDataType.Number).Build();
  }

  public override AresValueSchema VisitMulDiv(AresLangParser.MulDivContext context)
  {
    return NumericOrElse(context.expression(0), context.expression(1));
  }

  public override AresValueSchema VisitSub(AresLangParser.SubContext context)
  {
    return NumericOrElse(context.expression(0), context.expression(1));
  }

  public override AresValueSchema VisitAdd(AresLangParser.AddContext context)
  {
    
    return NumericOrElse(context.expression(0), context.expression(1), AresDataType.String);
  }

  public override AresValueSchema VisitRelational(AresLangParser.RelationalContext context)
  {
    return AresSchemaBuilder.Entry(AresDataType.Boolean).Build();
  }

  public override AresValueSchema VisitEquality(AresLangParser.EqualityContext context)
  {
    return AresSchemaBuilder.Entry(AresDataType.Boolean).Build();
  }

  public override AresValueSchema VisitLogicalNot(AresLangParser.LogicalNotContext context)
  {
    return AresSchemaBuilder.Entry(AresDataType.Boolean).Build();
  }

  public override AresValueSchema VisitLogicAnd(AresLangParser.LogicAndContext context)
  {
    return AresSchemaBuilder.Entry(AresDataType.Boolean).Build();
  }

  public override AresValueSchema VisitLogicOr(AresLangParser.LogicOrContext context)
  {
    return AresSchemaBuilder.Entry(AresDataType.Boolean).Build();
  }

  private AresValueSchema NumericOrElse(AresLangParser.ExpressionContext left, AresLangParser.ExpressionContext right, AresDataType elseType = AresDataType.Any)
  {
    var leftType = Visit(left);
    var rightType = Visit(right);
    if(leftType.Type == AresDataType.Number && rightType.Type == AresDataType.Number)
    {
      return AresSchemaBuilder.Entry(AresDataType.Number).Build();
    }

    return AresSchemaBuilder.Entry(elseType).Build();
  }

  private static AresValueSchema CreateStructEntry(AresStructSchema schema)
  {
    var entry = AresSchemaBuilder.Entry(AresDataType.Struct).Build();
    entry.StructSchema = schema;
    return entry;
  }

  private static AresValueSchema CreateListEntry(AresValueSchema elementSchema)
  {
    var entry = AresSchemaBuilder.Entry(AresDataType.List).Build();
    entry.ListElementSchema = elementSchema;
    return entry;
  }


  private string? TryResolveFunctionId(AresLangParser.ExpressionContext expression)
  {
    if(TryResolveValue(expression, out var value) && value?.FunctionValue is not null)
    {
      return value.FunctionValue.FunctionId;
    }

    var current = expression;
    while(true)
    {
      if(current is AresLangParser.AtomExprContext atomExpr)
      {
        switch(atomExpr.atom())
        {
          case AresLangParser.IdContext id:
            return id.ID().GetText();
          case AresLangParser.ParensContext parens:
            current = parens.expression();
            continue;
        }
      }

      return null;
    }
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

    return false;
  }

  private static string Unquote(string value)
  {
    if(value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
    {
      return value[1..^1];
    }

    return value;
  }
}
