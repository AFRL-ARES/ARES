using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using AresScript.Generated;
using System.Globalization;
using UnitsNet;

namespace AresScript;

internal static class AresScriptTypeHints
{
  public static IReadOnlyList<string> AvailableTypeNames { get; } = Enum.GetValues<AresDataType>()
    .Where(type => type != AresDataType.UnspecifiedType)
    .Select(type => type.ToString())
    .ToArray();

  public static bool TryParseTypeHint(AresLangParser.TypeHintContext? typeHint, out AresValueSchema schema)
    => TryParseTypeHint(typeHint, out schema, out _);

  public static bool TryParseTypeHint(AresLangParser.TypeHintContext? typeHint, out AresValueSchema schema, out string? error)
  {
    error = null;
    schema = AresSchemaBuilder.Entry(AresDataType.Any).Build();
    if(typeHint is null)
    {
      return true;
    }

    switch(typeHint)
    {
      case AresLangParser.NamedTypeRefContext namedTypeHint:
        if(!TryParseNamedTypeHint(namedTypeHint.namedTypeHint(), namedTypeHint.typeHintConstraints(), out schema, out error))
        {
          return false;
        }
        return true;

      case AresLangParser.StructTypeRefContext structTypeHint:
        {
          var structSchema = new AresStructSchema();
          foreach(var field in structTypeHint.structTypeHint().typeHintField())
          {
            if(!TryParseTypeHint(field.typeHint(), out var fieldSchema, out error))
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
        if(!TryParseTypeHint(listTypeHint.listTypeHint().typeHint(), out var elementSchema, out error))
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

  public static AresValueSchema SchemaFromTypeHint(AresLangParser.TypeHintContext? typeHint)
  {
    return TryParseTypeHint(typeHint, out var schema, out _)
      ? schema
      : AresSchemaBuilder.Entry(AresDataType.Any).Build();
  }

  public static AresValueSchema SchemaFromTypeHint(string? typeHint)
  {
    if(string.IsNullOrWhiteSpace(typeHint))
    {
      return AresSchemaBuilder.Entry(AresDataType.Any).Build();
    }

    var input = new Antlr4.Runtime.AntlrInputStream($"def __type_hint_probe(value: {typeHint}):\n  return value\n");
    var lexer = new AresIndentationLexer(input);
    var tokenStream = new Antlr4.Runtime.CommonTokenStream(lexer);
    var parser = new AresLangParser(tokenStream);
    var program = parser.program();
    if(parser.NumberOfSyntaxErrors > 0
      || program.statement().FirstOrDefault() is not AresLangParser.SimpleStmtContext simpleStatement
      || simpleStatement.simpleStatement() is not AresLangParser.FunctionDeclContext functionDecl)
    {
      return AresSchemaBuilder.Entry(AresDataType.Any).Build();
    }

    return SchemaFromTypeHint(functionDecl.functionDeclaration().parameterList()?.parameter().FirstOrDefault()?.typeHint());
  }

  public static bool IsCompatibleWithTypeHint(AresValueSchema actual, AresValueSchema expected)
  {
    return IsCompatible(expected, actual);
  }

  public static bool IsCompatibleWithTypeHint(AresValue actual, AresValueSchema expected)
  {
    if(!IsCompatibleWithTypeHint(actual.ToAresValueSchema(), expected))
    {
      return false;
    }

    if(expected.Type == AresDataType.Number && actual.HasNumberValue)
    {
      return IsCompatible(expected, actual.NumberValue);
    }

    if(expected.Type == AresDataType.Quantity && actual.KindCase == AresValue.KindOneofCase.QuantityValue)
    {
      return IsCompatible(expected.QuantitySchema, actual.QuantityValue);
    }

    return true;
  }

  public static bool IsCompatibleWithTypeHint(AresValueSchema actual, AresDataType expectedType)
  {
    return IsCompatibleWithTypeHint(actual, AresSchemaBuilder.Entry(expectedType).Build());
  }

  public static bool IsCompatibleWithTypeHint(AresValue actual, AresDataType expectedType)
  {
    return IsCompatibleWithTypeHint(actual.ToAresValueSchema(), expectedType);
  }

  private static bool IsCompatible(AresValueSchema expected, AresValueSchema actual)
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

  private static bool IsCompatible(AresValueSchema expected, double actual)
  {
    if(expected.HasMinNumberValue && actual < expected.MinNumberValue)
    {
      return false;
    }

    if(expected.HasMaxNumberValue && actual > expected.MaxNumberValue)
    {
      return false;
    }

    return true;
  }

  private static bool IsCompatible(QuantitySchema? expected, Ares.Datamodel.QuantityValue actualValue)
  {
    if(expected is null)
    {
      return true;
    }

    if(!actualValue.TryToUnitsNetQuantity(out var actualQuantity) || actualQuantity is null)
    {
      return false;
    }

    if(expected.QuantityType != QuantityType.Unspecified)
    {
      var expectedQuantityName = expected.QuantityType.ToUnitsNetQuantityName();
      if(!actualQuantity.QuantityInfo.Name.Equals(expectedQuantityName, StringComparison.OrdinalIgnoreCase))
      {
        return false;
      }
    }

    if((expected.HasMinScalarValue || expected.HasMaxScalarValue) && string.IsNullOrWhiteSpace(expected.BoundsUnit))
    {
      return false;
    }

    if(expected.HasMinScalarValue)
    {
      if(!TryConvertActualQuantityToBoundsScalar(actualQuantity, expected.BoundsUnit, out var comparableScalar)
        || comparableScalar < expected.MinScalarValue)
      {
        return false;
      }
    }

    if(expected.HasMaxScalarValue)
    {
      if(!TryConvertActualQuantityToBoundsScalar(actualQuantity, expected.BoundsUnit, out var comparableScalar)
        || comparableScalar > expected.MaxScalarValue)
      {
        return false;
      }
    }

    return true;
  }

  private static bool TryConvertActualQuantityToBoundsScalar(IQuantity actualQuantity, string boundsUnit, out double scalar)
  {
    UnitsNetAbbreviationExtensions.EnsureRegistered();

    scalar = 0;
    if(string.IsNullOrWhiteSpace(boundsUnit))
    {
      return false;
    }

    if(!UnitsNetAbbreviationExtensions.Parser.TryParse(boundsUnit, actualQuantity.QuantityInfo.UnitType, out Enum? parsedUnit))
    {
      return false;
    }

    scalar = actualQuantity.As(parsedUnit);
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
    UnitsNetAbbreviationExtensions.EnsureRegistered();

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
    if(enumUnit is null && !UnitsNetAbbreviationExtensions.Parser.TryParse(schema.BoundsUnit, quantityInfo.UnitType, out enumUnit))
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

  private static bool TryParseNamedTypeHint(
    AresLangParser.NamedTypeHintContext namedTypeHint,
    AresLangParser.TypeHintConstraintsContext? constraints,
    out AresValueSchema schema,
    out string? error)
  {
    error = null;
    schema = AresSchemaBuilder.Entry(AresDataType.Any).Build();
    var segments = namedTypeHint
      .GetText()
      .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if(segments.Length == 0)
    {
      error = "Type hint is empty.";
      return false;
    }

    var leafName = segments[^1];
    var isQuantityWithTypedSuffix = segments.Length >= 2
      && string.Equals(segments[^2], nameof(AresDataType.Quantity), StringComparison.OrdinalIgnoreCase);
    var dataTypeToken = isQuantityWithTypedSuffix ? nameof(AresDataType.Quantity) : leafName;
    if(!Enum.TryParse(dataTypeToken, true, out AresDataType dataType) || dataType == AresDataType.UnspecifiedType)
    {
      error = $"'{namedTypeHint.GetText()}' is not a known ARES type hint.";
      return false;
    }

    schema = AresSchemaBuilder.Entry(dataType).Build();
    if(dataType == AresDataType.Quantity)
    {
      schema.QuantitySchema = new QuantitySchema();
      if(isQuantityWithTypedSuffix)
      {
        if(!Enum.TryParse<QuantityType>(leafName, true, out var quantityType)
          || quantityType == QuantityType.Unspecified)
        {
          error = $"'{leafName}' is not a known quantity type.";
          return false;
        }

        schema.QuantitySchema.QuantityType = quantityType;
      }
    }

    if(constraints is null)
    {
      return true;
    }

    return ApplyConstraints(schema, constraints, out error);
  }

  private static bool ApplyConstraints(AresValueSchema schema, AresLangParser.TypeHintConstraintsContext constraints, out string? error)
  {
    error = null;
    var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach(var constraint in constraints.typeHintConstraint())
    {
      var key = constraint.ID()?.GetText();
      if(key is null)
      {
        error = "Constraint key is missing.";
        return false;
      }
      if(!seenKeys.Add(key))
      {
        error = $"Constraint '{key}' is specified more than once.";
        return false;
      }

      if(!ApplyConstraint(schema, key, constraint.typeHintConstraintValue(), out error))
      {
        return false;
      }
    }

    if(schema.Type == AresDataType.Number)
    {
      if(schema.HasMinNumberValue && schema.HasMaxNumberValue && schema.MinNumberValue > schema.MaxNumberValue)
      {
        error = $"Number type hint has min {schema.MinNumberValue} greater than max {schema.MaxNumberValue}.";
        return false;
      }
    }

    if(schema.Type == AresDataType.Quantity)
    {
      schema.QuantitySchema ??= new QuantitySchema();
      var quantitySchema = schema.QuantitySchema;
      if((quantitySchema.HasMinScalarValue || quantitySchema.HasMaxScalarValue)
        && string.IsNullOrWhiteSpace(quantitySchema.BoundsUnit))
      {
        error = "Quantity type hints with min/max constraints must specify a bounds unit using unit=\"...\".";
        return false;
      }

      if(quantitySchema.HasMinScalarValue
        && quantitySchema.HasMaxScalarValue
        && quantitySchema.MinScalarValue > quantitySchema.MaxScalarValue)
      {
        error = $"Quantity type hint has min {quantitySchema.MinScalarValue} greater than max {quantitySchema.MaxScalarValue}.";
        return false;
      }

      if(quantitySchema.QuantityType != QuantityType.Unspecified
        && !string.IsNullOrWhiteSpace(quantitySchema.BoundsUnit)
        && !IsValidBoundsUnit(quantitySchema.QuantityType, quantitySchema.BoundsUnit, out error))
      {
        return false;
      }
    }

    return true;
  }

  private static bool ApplyConstraint(
    AresValueSchema schema,
    string key,
    AresLangParser.TypeHintConstraintValueContext valueContext,
    out string? error)
  {
    error = null;
    if(schema.Type == AresDataType.Number)
    {
      return ApplyNumberConstraint(schema, key, valueContext, out error);
    }

    if(schema.Type == AresDataType.Quantity)
    {
      return ApplyQuantityConstraint(schema, key, valueContext, out error);
    }

    error = $"Type '{schema.Type}' does not support constraints.";
    return false;
  }

  private static bool ApplyNumberConstraint(
    AresValueSchema schema,
    string key,
    AresLangParser.TypeHintConstraintValueContext valueContext,
    out string? error)
  {
    error = null;
    if(!TryReadNumericConstraintValue(valueContext, out var number))
    {
      error = $"Number constraint '{key}' requires a numeric value.";
      return false;
    }

    if(string.Equals(key, "min", StringComparison.OrdinalIgnoreCase))
    {
      schema.MinNumberValue = number;
      return true;
    }

    if(string.Equals(key, "max", StringComparison.OrdinalIgnoreCase))
    {
      schema.MaxNumberValue = number;
      return true;
    }

    error = $"Unknown Number constraint '{key}'. Supported constraints are min and max.";
    return false;
  }

  private static bool ApplyQuantityConstraint(
    AresValueSchema schema,
    string key,
    AresLangParser.TypeHintConstraintValueContext valueContext,
    out string? error)
  {
    error = null;
    schema.QuantitySchema ??= new QuantitySchema();
    var quantitySchema = schema.QuantitySchema;

    if(string.Equals(key, "unit", StringComparison.OrdinalIgnoreCase))
    {
      if(!TryReadStringConstraintValue(valueContext, out var unit))
      {
        error = "Quantity constraint 'unit' requires a non-empty string value.";
        return false;
      }

      quantitySchema.BoundsUnit = unit;
      return true;
    }

    if(!TryReadNumericConstraintValue(valueContext, out var number))
    {
      error = $"Quantity constraint '{key}' requires a numeric value.";
      return false;
    }

    if(string.Equals(key, "min", StringComparison.OrdinalIgnoreCase))
    {
      quantitySchema.MinScalarValue = number;
      return true;
    }

    if(string.Equals(key, "max", StringComparison.OrdinalIgnoreCase))
    {
      quantitySchema.MaxScalarValue = number;
      return true;
    }

    error = $"Unknown Quantity constraint '{key}'. Supported constraints are unit, min, and max.";
    return false;
  }

  private static bool TryReadStringConstraintValue(AresLangParser.TypeHintConstraintValueContext valueContext, out string value)
  {
    value = string.Empty;
    var raw = valueContext?.STRING()?.GetText();
    if(string.IsNullOrWhiteSpace(raw))
    {
      return false;
    }

    value = Unquote(raw);
    return !string.IsNullOrWhiteSpace(value);
  }

  private static bool TryReadNumericConstraintValue(AresLangParser.TypeHintConstraintValueContext valueContext, out double number)
  {
    number = default;
    var signedNumber = valueContext?.signedNumber();
    if(signedNumber is null)
    {
      return false;
    }

    var raw = signedNumber.GetText().Replace("_", string.Empty, StringComparison.Ordinal);
    return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out number);
  }

  private static string Unquote(string text)
  {
    if(text.Length >= 2 && ((text[0] == '"' && text[^1] == '"') || (text[0] == '\'' && text[^1] == '\'')))
    {
      return text[1..^1];
    }

    return text;
  }

  private static bool IsValidBoundsUnit(QuantityType quantityType, string boundsUnit, out string? error)
  {
    UnitsNetAbbreviationExtensions.EnsureRegistered();

    error = null;
    try
    {
      var unitsNetQuantityName = quantityType.ToUnitsNetQuantityName();
      var quantityInfo = Quantity.Infos.FirstOrDefault(
        info => info.Name.Equals(unitsNetQuantityName, StringComparison.OrdinalIgnoreCase));
      if(quantityInfo is null)
      {
        error = $"No UnitsNet quantity mapping exists for QuantityType '{quantityType}'.";
        return false;
      }

      var exactMatch = quantityInfo.UnitInfos
        .Select(unitInfo => unitInfo.Value)
        .OfType<Enum>()
        .Any(unit => unit.ToString().Equals(boundsUnit, StringComparison.OrdinalIgnoreCase));
      if(exactMatch)
      {
        return true;
      }

      if(UnitsNetAbbreviationExtensions.Parser.TryParse(boundsUnit, quantityInfo.UnitType, out Enum? parsedUnit) && parsedUnit is not null)
      {
        return true;
      }

      error = $"Unit '{boundsUnit}' is not valid for quantity type '{quantityType}'.";
      return false;
    }
    catch(InvalidOperationException ex)
    {
      error = ex.Message;
      return false;
    }
  }

  private static bool AreStructSchemasCompatible(AresStructSchema? expected, AresStructSchema? actual)
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
