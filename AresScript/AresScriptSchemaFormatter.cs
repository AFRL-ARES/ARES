using Ares.Datamodel;
using System.Globalization;
using System.Text;

namespace AresScript;

internal static class AresScriptSchemaFormatter
{
  public static string ToTypeHint(AresValueSchema schema)
  {
    return schema.Type switch
    {
      AresDataType.Struct => FormatStruct(schema),
      AresDataType.List => FormatList(schema),
      AresDataType.Quantity => FormatQuantity(schema),
      AresDataType.Number => AppendConstraints(nameof(AresDataType.Number), FormatNumberConstraints(schema)),
      _ => FormatNamedType(schema.Type)
    };
  }

  private static string FormatNamedType(AresDataType type)
  {
    return type switch
    {
      AresDataType.UnspecifiedType => nameof(AresDataType.Any),
      _ => type.ToString()
    };
  }

  private static string FormatStruct(AresValueSchema schema)
  {
    if(schema.StructSchema is null || schema.StructSchema.Fields.Count == 0)
    {
      return "{}";
    }

    var fields = schema.StructSchema.Fields
      .Select(field => $"{field.Key}: {ToTypeHint(field.Value)}");
    return $"{{{string.Join(", ", fields)}}}";
  }

  private static string FormatList(AresValueSchema schema)
  {
    var elementType = schema.ListElementSchema is null
      ? nameof(AresDataType.Any)
      : ToTypeHint(schema.ListElementSchema);
    return $"[{elementType}]";
  }

  private static string FormatQuantity(AresValueSchema schema)
  {
    var quantitySchema = schema.QuantitySchema;
    var baseType = quantitySchema is not null && quantitySchema.QuantityType != QuantityType.Unspecified
      ? $"{nameof(AresDataType.Quantity)}.{quantitySchema.QuantityType}"
      : nameof(AresDataType.Quantity);
    return AppendConstraints(baseType, FormatQuantityConstraints(quantitySchema));
  }

  private static IReadOnlyList<string> FormatNumberConstraints(AresValueSchema schema)
  {
    var constraints = new List<string>();
    if(schema.HasMinNumberValue)
    {
      constraints.Add($"min={FormatNumber(schema.MinNumberValue)}");
    }

    if(schema.HasMaxNumberValue)
    {
      constraints.Add($"max={FormatNumber(schema.MaxNumberValue)}");
    }

    return constraints;
  }

  private static IReadOnlyList<string> FormatQuantityConstraints(QuantitySchema? schema)
  {
    if(schema is null)
    {
      return [];
    }

    var constraints = new List<string>();
    if(!string.IsNullOrWhiteSpace(schema.BoundsUnit))
    {
      constraints.Add($"unit={FormatStringLiteral(schema.BoundsUnit)}");
    }

    if(schema.HasMinScalarValue)
    {
      constraints.Add($"min={FormatNumber(schema.MinScalarValue)}");
    }

    if(schema.HasMaxScalarValue)
    {
      constraints.Add($"max={FormatNumber(schema.MaxScalarValue)}");
    }

    return constraints;
  }

  private static string AppendConstraints(string baseType, IReadOnlyList<string> constraints)
  {
    return constraints.Count == 0
      ? baseType
      : $"{baseType}[{string.Join(", ", constraints)}]";
  }

  private static string FormatNumber(double value)
  {
    return value.ToString("0.###############################", CultureInfo.InvariantCulture);
  }

  private static string FormatStringLiteral(string value)
  {
    var builder = new StringBuilder(value.Length + 2);
    builder.Append('"');
    foreach(var ch in value)
    {
      switch(ch)
      {
        case '\\':
          builder.Append("\\\\");
          break;
        case '"':
          builder.Append("\\\"");
          break;
        default:
          builder.Append(ch);
          break;
      }
    }

    builder.Append('"');
    return builder.ToString();
  }
}
