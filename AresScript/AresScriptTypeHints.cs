using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;

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

  public static SchemaEntry SchemaFromTypeHint(string? typeHint)
  {
    return TryParseTypeHint(typeHint, out var parsedType)
      ? AresSchemaBuilder.Entry(parsedType).Build()
      : AresSchemaBuilder.Entry(AresDataType.Any).Build();
  }

  public static bool IsCompatibleWithTypeHint(SchemaEntry actual, AresDataType expectedType)
  {
    if(expectedType == AresDataType.Any || expectedType == AresDataType.UnspecifiedType)
    {
      return true;
    }

    if(actual.Type == AresDataType.Any || actual.Type == AresDataType.UnspecifiedType)
    {
      return true;
    }

    if(expectedType == actual.Type)
    {
      return true;
    }

    return false;
  }

  public static bool IsCompatibleWithTypeHint(AresValue actual, AresDataType expectedType)
  {
    return IsCompatibleWithTypeHint(actual.ToSchemaEntry(), expectedType);
  }
}
