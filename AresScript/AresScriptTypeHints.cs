using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using AresScript.Generated;
using UnitsNet;

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
      case AresDataType.Quantity:
        return IsCompatible(expected.QuantitySchema, actual.QuantitySchema);
      default:
        return true;
    }
  }

  private static bool IsCompatible(QuantitySchema? expected, QuantitySchema? actual)
  {
    if(expected is null || actual is null)
    {
      return true;
    }

    if(expected.QuantityType != QuantityType.Unspecified
      && actual.QuantityType != QuantityType.Unspecified
      && expected.QuantityType != actual.QuantityType)
    {
      return false;
    }

    if(expected.HasMinScalarValue
      && actual.HasMinScalarValue
      && !IsQuantityBoundCompatible(expected, actual, isMinBound: true))
    {
      return false;
    }

    if(expected.HasMaxScalarValue
      && actual.HasMaxScalarValue
      && !IsQuantityBoundCompatible(expected, actual, isMinBound: false))
    {
      return false;
    }

    return true;
  }

  private static bool IsQuantityBoundCompatible(
    QuantitySchema expectedSchema,
    QuantitySchema actualSchema,
    bool isMinBound)
  {
    var expectedScalar = isMinBound ? expectedSchema.MinScalarValue : expectedSchema.MaxScalarValue;
    var actualScalar = isMinBound ? actualSchema.MinScalarValue : actualSchema.MaxScalarValue;

    if(TryConvertToComparableScalars(expectedSchema, actualSchema, isMinBound, out var expectedComparable, out var actualComparable))
    {
      return isMinBound
        ? actualComparable >= expectedComparable
        : actualComparable <= expectedComparable;
    }

    return isMinBound
      ? actualScalar >= expectedScalar
      : actualScalar <= expectedScalar;
  }

  private static bool TryConvertToComparableScalars(
    QuantitySchema expectedSchema,
    QuantitySchema actualSchema,
    bool isMinBound,
    out double expectedComparable,
    out double actualComparable)
  {
    var expectedScalar = isMinBound ? expectedSchema.MinScalarValue : expectedSchema.MaxScalarValue;
    var actualScalar = isMinBound ? actualSchema.MinScalarValue : actualSchema.MaxScalarValue;

    expectedComparable = expectedScalar;
    actualComparable = actualScalar;

    if(!TryToUnitsNetQuantity(expectedSchema, actualSchema, expectedScalar, out var expectedQuantity))
    {
      return false;
    }

    if(!TryToUnitsNetQuantity(actualSchema, expectedSchema, actualScalar, out var actualQuantity))
    {
      return false;
    }

    var expectedBaseUnit = expectedQuantity.QuantityInfo.BaseUnitInfo.Value;
    var actualBaseUnit = actualQuantity.QuantityInfo.BaseUnitInfo.Value;
    expectedComparable = expectedQuantity.As(expectedBaseUnit);
    actualComparable = actualQuantity.As(actualBaseUnit);
    return true;
  }

  private static bool TryToUnitsNetQuantity(
    QuantitySchema schema,
    QuantitySchema otherSchema,
    double scalar,
    out IQuantity quantity)
  {
    quantity = default!;

    if(string.IsNullOrWhiteSpace(schema.BoundsUnit))
    {
      return false;
    }

    var quantityType = ResolveQuantityType(schema, otherSchema);
    if(quantityType == QuantityType.Unspecified)
    {
      return false;
    }

    var unitsNetQuantityName = quantityType.ToUnitsNetQuantityName();
    var quantityInfo = Quantity.Infos.FirstOrDefault(info => info.Name.Equals(unitsNetQuantityName, StringComparison.OrdinalIgnoreCase));
    if(quantityInfo is null)
    {
      return false;
    }

    var enumUnit = quantityInfo.UnitInfos
      .Select(unitInfo => unitInfo.Value)
      .FirstOrDefault(u => u.ToString().Equals(schema.BoundsUnit, StringComparison.OrdinalIgnoreCase));

    // Try strict enum-name matching first, then fall back to UnitsNet parsing so
    // aliases/abbreviations (e.g. "s", "sec") can still resolve correctly.
    if(enumUnit is null && !UnitParser.Default.TryParse(schema.BoundsUnit, quantityInfo.UnitType, out enumUnit))
    {
      return false;
    }

    if(enumUnit is null)
    {
      return false;
    }

    quantity = Quantity.From(scalar, enumUnit);
    return true;
  }

  private static QuantityType ResolveQuantityType(QuantitySchema schema, QuantitySchema otherSchema)
  {
    if(schema.QuantityType != QuantityType.Unspecified)
    {
      return schema.QuantityType;
    }

    return otherSchema.QuantityType != QuantityType.Unspecified
      ? otherSchema.QuantityType
      : QuantityType.Unspecified;
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
