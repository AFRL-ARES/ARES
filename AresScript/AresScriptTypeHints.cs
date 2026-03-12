using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using AresScript.Generated;

namespace AresScript;

internal static class AresScriptTypeHints
{
  public static IReadOnlyList<string> AvailableTypeNames { get; } = Enum.GetValues<AresDataType>()
    .Where(type => type != AresDataType.UnspecifiedType)
    .Select(type => type.ToString())
    .ToArray();

  public static bool TryParseTypeHint(string? typeHint, out AresDataType type)
  {
    type = AresDataType.Any;
    if(string.IsNullOrWhiteSpace(typeHint))
    {
      return false;
    }

    var leafName = typeHint.Trim().Split('.').Last();
    if(!Enum.TryParse(leafName, true, out AresDataType parsedType))
    {
      return false;
    }

    if(parsedType == AresDataType.UnspecifiedType)
    {
      return false;
    }

    type = parsedType;
    return true;
  }

  public static bool TryParseTypeHint(AresLangParser.TypeHintContext? typeHint, out SchemaEntry schema)
  {
    schema = AresSchemaBuilder.Entry(AresDataType.Any).Build();
    if(typeHint is null)
    {
      return true;
    }

    switch(typeHint)
    {
      case AresLangParser.NamedTypeRefContext namedTypeHint:
        if(!TryParseTypeHint(namedTypeHint.namedTypeHint().GetText(), out var resolvedType))
        {
          return false;
        }

        schema = AresSchemaBuilder.Entry(resolvedType).Build();
        return true;

      case AresLangParser.StructTypeRefContext structTypeHint:
        {
          var structSchema = new AresDataSchema();
          foreach(var field in structTypeHint.structTypeHint().typeHintField())
          {
            if(!TryParseTypeHint(field.typeHint(), out var fieldSchema))
            {
              return false;
            }

            structSchema.Fields[field.ID().GetText()] = fieldSchema;
          }

          schema = AresSchemaBuilder.Entry(AresDataType.Struct).Build();
          schema.StructSchema = structSchema;
          return true;
        }

      case AresLangParser.ListTypeRefContext listTypeHint:
        if(!TryParseTypeHint(listTypeHint.listTypeHint().typeHint(), out var elementSchema))
        {
          return false;
        }

        schema = AresSchemaBuilder.Entry(AresDataType.List).Build();
        schema.ListElementSchema = elementSchema;
        return true;

      default:
        return false;
    }
  }

  public static SchemaEntry SchemaFromTypeHint(AresLangParser.TypeHintContext? typeHint)
  {
    return TryParseTypeHint(typeHint, out var schema)
      ? schema
      : AresSchemaBuilder.Entry(AresDataType.Any).Build();
  }

  public static SchemaEntry SchemaFromTypeHint(string? typeHint)
  {
    return TryParseTypeHint(typeHint, out var parsedType)
      ? AresSchemaBuilder.Entry(parsedType).Build()
      : AresSchemaBuilder.Entry(AresDataType.Any).Build();
  }

  public static bool IsCompatibleWithTypeHint(SchemaEntry actual, SchemaEntry expected)
  {
    return IsCompatible(expected, actual);
  }

  public static bool IsCompatibleWithTypeHint(AresValue actual, SchemaEntry expected)
  {
    return IsCompatibleWithTypeHint(actual.ToSchemaEntry(), expected);
  }

  public static bool IsCompatibleWithTypeHint(SchemaEntry actual, AresDataType expectedType)
  {
    return IsCompatibleWithTypeHint(actual, AresSchemaBuilder.Entry(expectedType).Build());
  }

  public static bool IsCompatibleWithTypeHint(AresValue actual, AresDataType expectedType)
  {
    return IsCompatibleWithTypeHint(actual.ToSchemaEntry(), expectedType);
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

    if(expected.Type != actual.Type)
    {
      return false;
    }

    switch(expected.Type)
    {
      case AresDataType.Struct:
        return AreStructSchemasCompatible(expected.StructSchema, actual.StructSchema);
      case AresDataType.List:
        if(expected.ListElementSchema is null || actual.ListElementSchema is null)
        {
          return true;
        }

        return IsCompatible(expected.ListElementSchema, actual.ListElementSchema);
      default:
        return true;
    }
  }

  private static bool AreStructSchemasCompatible(AresDataSchema? expected, AresDataSchema? actual)
  {
    if(expected is null || expected.Fields.Count == 0)
    {
      return true;
    }

    if(actual is null)
    {
      return false;
    }

    foreach(var (name, expectedField) in expected.Fields)
    {
      if(!actual.Fields.TryGetValue(name, out var actualField))
      {
        if(expectedField.Optional)
        {
          continue;
        }

        return false;
      }

      if(!IsCompatible(expectedField, actualField))
      {
        return false;
      }
    }

    return true;
  }
}
