using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Datamodel.Scripting;

namespace AresScript.Symbols;

public static class ScriptSymbolMetadataMapper
{
  public static ScriptSymbolMetadata ToMetadata(
    IScriptSymbol symbol,
    string? parentIdentifier = null,
    string? detail = null,
    string? documentation = null,
    AresValueSchema? valueSchema = null,
    AresValue? value = null)
  {
    ArgumentNullException.ThrowIfNull(symbol);
    parentIdentifier ??= symbol.ParentName;
    detail ??= symbol.Detail;
    documentation ??= symbol.Documentation;

    var metadata = new ScriptSymbolMetadata
    {
      Identifier = symbol.Name ?? string.Empty,
      Kind = symbol.SymbolKind
    };

    if(!string.IsNullOrWhiteSpace(parentIdentifier))
    {
      metadata.ParentIdentifier = parentIdentifier;
    }

    if(!string.IsNullOrWhiteSpace(detail))
    {
      metadata.Detail = detail;
    }

    if(!string.IsNullOrWhiteSpace(documentation))
    {
      metadata.Documentation = documentation;
    }

    metadata.Tags.AddRange(ToProtoTags(symbol));

    switch(symbol)
    {
      case AresSystemFunctionSymbol systemFunction:
        metadata.FunctionShape = new ScriptSymbolMetadata.Types.FunctionShape
        {
          InputSchema = systemFunction.InputSchema,
          OutputSchema = systemFunction.OutputSchema
        };
        break;

      case AresScriptFunction userFunction:
        metadata.FunctionShape = new ScriptSymbolMetadata.Types.FunctionShape
        {
          InputSchema = BuildUserFunctionInputSchema(userFunction),
          OutputSchema = userFunction.ReturnSchema
        };
        break;

      case AresScriptLambda lambda:
        metadata.FunctionShape = new ScriptSymbolMetadata.Types.FunctionShape
        {
          InputSchema = BuildLambdaInputSchema(lambda),
          OutputSchema = AresSchemaBuilder.Entry(AresDataType.Any).Build()
        };
        break;

      case AresScriptValueSymbol scriptValueSymbol:
      {
        var resolvedValue = value ?? scriptValueSymbol.Value;
        var resolvedSchema = valueSchema ?? resolvedValue.ToAresValueSchema();
        metadata.ValueShape = new ScriptSymbolMetadata.Types.ValueShape
        {
          Schema = resolvedSchema,
          Value = resolvedValue
        };
        break;
      }

      case AresSystemValue systemValueSymbol:
      {
        var resolvedValue = value ?? systemValueSymbol.Value;
        var resolvedSchema = valueSchema ?? systemValueSymbol.DeclaredSchema ?? resolvedValue.ToAresValueSchema();
        metadata.ValueShape = new ScriptSymbolMetadata.Types.ValueShape
        {
          Schema = resolvedSchema,
          Value = resolvedValue
        };
        break;
      }

      case IValueSymbol valueSymbol:
      {
        var resolvedValue = value ?? valueSymbol.Value;
        var resolvedSchema = valueSchema ?? resolvedValue.ToAresValueSchema();
        if(resolvedSchema is null && symbol.SymbolKind is SymbolKind.Variable or SymbolKind.Struct)
        {
          resolvedSchema = new AresValueSchema { Type = AresDataType.UnspecifiedType };
        }

        if(resolvedSchema is not null || resolvedValue is not null)
        {
          metadata.ValueShape = new ScriptSymbolMetadata.Types.ValueShape();
          if(resolvedSchema is not null)
          {
            metadata.ValueShape.Schema = resolvedSchema;
          }

          if(resolvedValue is not null)
          {
            metadata.ValueShape.Value = resolvedValue;
          }
        }
        break;
      }
    }

    return metadata;
  }

  private static IEnumerable<SymbolTag> ToProtoTags(IScriptSymbol symbol)
  {
    if(symbol is IFunctionSymbol function)
    {
      if(function.IsExtension)
      {
        yield return SymbolTag.Extension;
      }

      if(function.IsUserDefined)
      {
        yield return SymbolTag.UserDefined;
      }

      if(function.IsLambda)
      {
        yield return SymbolTag.Lambda;
      }
    }

    if(symbol is IValueSymbol value && value.IsReadOnly)
    {
      yield return SymbolTag.ReadOnly;
    }
  }

  private static AresStructSchema BuildUserFunctionInputSchema(AresScriptFunction userFunction)
  {
    var schema = new AresStructSchema();
    foreach(var parameter in userFunction.Parameters)
    {
      schema.Fields[parameter.Name] = parameter.Schema;
    }

    return schema;
  }

  private static AresStructSchema BuildLambdaInputSchema(AresScriptLambda lambda)
  {
    var schema = new AresStructSchema();
    foreach(var parameter in lambda.Parameters)
    {
      schema.Fields[parameter] = AresSchemaBuilder.Entry(AresDataType.Any).Build();
    }

    return schema;
  }
}
