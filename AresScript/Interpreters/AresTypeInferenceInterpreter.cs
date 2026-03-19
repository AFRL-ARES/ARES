using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using AresScript.Environment;
using AresScript.Generated;

namespace AresScript.Interpreters;

/// <summary>
/// Interpreter specifically to gather the types of symbols so they can be displayed via hover
/// or otherwise validated when used as inputs/outputs
/// </summary>
public sealed class AresTypeInferenceInterpreter : AresLangBaseVisitor<SchemaEntry>
{
  private readonly AresScriptEnvironment _environment;

  public AresTypeInferenceInterpreter(AresScriptEnvironment environment)
  {
    _environment = environment;
  }

  protected override SchemaEntry DefaultResult => AresSchemaBuilder.Entry(AresDataType.Any).Build();
  public override SchemaEntry VisitInt(AresLangParser.IntContext context) => AresSchemaBuilder.Entry(AresDataType.Number).Build();
  public override SchemaEntry VisitFloat(AresLangParser.FloatContext context) => AresSchemaBuilder.Entry(AresDataType.Number).Build();
  public override SchemaEntry VisitString(AresLangParser.StringContext context) => AresSchemaBuilder.Entry(AresDataType.String).Build();
  public override SchemaEntry VisitBool(AresLangParser.BoolContext context) => AresSchemaBuilder.Entry(AresDataType.Boolean).Build();
  public override SchemaEntry VisitNone(AresLangParser.NoneContext context) => AresSchemaBuilder.Entry(AresDataType.Null).Build();

  public override SchemaEntry VisitId(AresLangParser.IdContext context)
  {
    var id = context.ID().GetText();
    if(_environment.TryGetSystemFunction(id, out var sysFunc) && sysFunc?.OutputSchema is not null) 
    {
      return sysFunc.OutputSchema;
    }
    if(_environment.TryGetUserFunction(id, out var _))
    {
      return AresSchemaBuilder.Entry(AresDataType.Function).Build();
    }
    if(_environment.TryGetUserLambda(id, out var _))
    {
      return AresSchemaBuilder.Entry(AresDataType.Function).Build();
    }

    if(_environment.TryGetValueSymbol(id, out var envSymbol))
    {
      return envSymbol.DeclaredSchema ?? envSymbol.Value.ToSchemaEntry();
    }

    return AresSchemaBuilder.Entry(AresDataType.Any).Build();
  }

  public override SchemaEntry VisitParens(AresLangParser.ParensContext context)
  {
    return Visit(context.expression());
  }

  public override SchemaEntry VisitArray(AresLangParser.ArrayContext context)
  {
    var expressions = context.expression();
    if(expressions.Length == 0)
    {
      return CreateListEntry(AresSchemaBuilder.Entry(AresDataType.Any).Build());
    }

    var elementTypes = expressions.Select(Visit).ToArray();
    var firstElementSchema = elementTypes[0];
    var firstType = firstElementSchema.Type;
    var allSameSchema = elementTypes.All(schema => AreEquivalentSchemas(firstElementSchema, schema));

    if(allSameSchema)
    {
      if(firstType == AresDataType.String)
      {
        return AresSchemaBuilder.Entry(AresDataType.StringArray).Build();
      }

      if(firstType == AresDataType.Number)
      {
        return AresSchemaBuilder.Entry(AresDataType.NumberArray).Build();
      }

      return CreateListEntry(firstElementSchema);
    }

    return CreateListEntry(AresSchemaBuilder.Entry(AresDataType.Any).Build());
  }

  public override SchemaEntry VisitLambdaExpr(AresLangParser.LambdaExprContext context)
  {
    return AresSchemaBuilder.Entry(AresDataType.Function).Build();
  }

  public override SchemaEntry VisitStruct(AresLangParser.StructContext context)
  {
    var schema = new AresDataSchema();
    foreach(var pair in context.structure().pair())
    {
      var key = pair.ID()?.GetText() ?? Unquote(pair.STRING().GetText());
      var value = Visit(pair.expression());
      schema.Fields[key] = value;
    }

    return CreateStructEntry(schema);
  }

  public override SchemaEntry VisitMemberAccess(AresLangParser.MemberAccessContext context)
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

  public override SchemaEntry VisitIndexAccess(AresLangParser.IndexAccessContext context)
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

  public override SchemaEntry VisitFunctionCall(AresLangParser.FunctionCallContext context)
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
    if(functionId is not null && _environment.TryGetUserFunction(functionId, out var userFunc))
    {
      return userFunc.ReturnSchema;
    }
    if(functionId is not null && _environment.TryGetUserLambda(functionId, out _))
    {
      return AresSchemaBuilder.Entry(AresDataType.Any).Build();
    }

    return AresSchemaBuilder.Entry(AresDataType.Any).Build();
  }

  public override SchemaEntry VisitUnaryMinus(AresLangParser.UnaryMinusContext context)
  {
    var operand = Visit(context.expression());
    if(operand.Type == AresDataType.Quantity)
    {
      return CreateQuantityResultEntry(operand);
    }

    return AresSchemaBuilder.Entry(AresDataType.Number).Build();
  }

  public override SchemaEntry VisitMulDiv(AresLangParser.MulDivContext context)
  {
    return NumericOrQuantityOrElse(context.expression(0), context.expression(1));
  }

  public override SchemaEntry VisitSub(AresLangParser.SubContext context)
  {
    return NumericOrQuantityOrElse(context.expression(0), context.expression(1), allowRightNumberForQuantityLeft: false);
  }

  public override SchemaEntry VisitAdd(AresLangParser.AddContext context)
  {
    return NumericOrQuantityOrElse(context.expression(0), context.expression(1), allowRightNumberForQuantityLeft: false, elseType: AresDataType.String);
  }

  public override SchemaEntry VisitRelational(AresLangParser.RelationalContext context)
  {
    return AresSchemaBuilder.Entry(AresDataType.Boolean).Build();
  }

  public override SchemaEntry VisitEquality(AresLangParser.EqualityContext context)
  {
    return AresSchemaBuilder.Entry(AresDataType.Boolean).Build();
  }

  public override SchemaEntry VisitLogicalNot(AresLangParser.LogicalNotContext context)
  {
    return AresSchemaBuilder.Entry(AresDataType.Boolean).Build();
  }

  public override SchemaEntry VisitLogicAnd(AresLangParser.LogicAndContext context)
  {
    return AresSchemaBuilder.Entry(AresDataType.Boolean).Build();
  }

  public override SchemaEntry VisitLogicOr(AresLangParser.LogicOrContext context)
  {
    return AresSchemaBuilder.Entry(AresDataType.Boolean).Build();
  }

  private SchemaEntry NumericOrQuantityOrElse(
    AresLangParser.ExpressionContext left,
    AresLangParser.ExpressionContext right,
    bool allowRightNumberForQuantityLeft = true,
    AresDataType elseType = AresDataType.Any)
  {
    var leftType = Visit(left);
    var rightType = Visit(right);
    if(leftType.Type == AresDataType.Number && rightType.Type == AresDataType.Number)
    {
      return AresSchemaBuilder.Entry(AresDataType.Number).Build();
    }

    if(leftType.Type == AresDataType.Quantity)
    {
      if((allowRightNumberForQuantityLeft && rightType.Type == AresDataType.Number)
        || (rightType.Type == AresDataType.Quantity && AreQuantitySchemasCompatible(leftType.QuantitySchema, rightType.QuantitySchema))
        || rightType.Type is AresDataType.Any or AresDataType.UnspecifiedType)
      {
        return CreateQuantityResultEntry(leftType, rightType);
      }
    }

    return AresSchemaBuilder.Entry(elseType).Build();
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

  private static SchemaEntry CreateQuantityResultEntry(params SchemaEntry[] candidates)
  {
    var quantityType = candidates
      .Select(candidate => candidate.QuantitySchema?.QuantityType ?? QuantityType.Unspecified)
      .FirstOrDefault(type => type != QuantityType.Unspecified);

    return new SchemaEntry
    {
      Type = AresDataType.Quantity,
      QuantitySchema = new QuantitySchema
      {
        QuantityType = quantityType
      }
    };
  }

  private static bool AreEquivalentSchemas(SchemaEntry left, SchemaEntry right)
  {
    return AresScriptTypeHints.IsCompatibleWithTypeHint(left, right)
      && AresScriptTypeHints.IsCompatibleWithTypeHint(right, left);
  }

  private static SchemaEntry CreateStructEntry(AresDataSchema schema)
  {
    var entry = AresSchemaBuilder.Entry(AresDataType.Struct).Build();
    entry.StructSchema = schema;
    return entry;
  }

  private static SchemaEntry CreateListEntry(SchemaEntry elementSchema)
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
