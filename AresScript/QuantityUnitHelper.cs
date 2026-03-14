using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using System.Diagnostics.CodeAnalysis;
using UnitsNet;

namespace AresScript;

public static class QuantityUnitHelper
{
  public static bool TryParseUnit(QuantityType quantityType, string unitText, out Enum? unit, out string? error)
  {
    unit = null;
    error = null;

    if(string.IsNullOrWhiteSpace(unitText))
    {
      error = "Unit must be a non-empty string.";
      return false;
    }

    try
    {
      var quantityInfo = ResolveQuantityInfo(quantityType);

      unit = quantityInfo.UnitInfos
        .Select(unitInfo => unitInfo.Value)
        .FirstOrDefault(candidate => candidate.ToString().Equals(unitText, StringComparison.OrdinalIgnoreCase));

      if(unit is not null)
      {
        return true;
      }

      if(UnitParser.Default.TryParse(unitText, quantityInfo.UnitType, out Enum? parsedUnit))
      {
        unit = parsedUnit;
        return true;
      }

      error = $"Unit '{unitText}' is not valid for quantity type '{quantityType}'.";
      return false;
    }
    catch(InvalidOperationException ex)
    {
      error = ex.Message;
      return false;
    }
  }

  public static bool IsValidUnit(QuantityType quantityType, string unitText, out string? error)
  {
    return TryParseUnit(quantityType, unitText, out _, out error);
  }

  public static bool TryValidateConstructionArgs(
    QuantityType quantityType,
    AresValue? valueArg,
    AresValue? unitArg,
    out double scalar,
    [NotNullWhen(true)]
    out Enum? unit,
    out string? error)
  {
    scalar = 0;
    unit = null;
    error = null;

    if(valueArg is null || !valueArg.HasNumberValue)
    {
      error = "expected first argument 'value' to be a number.";
      return false;
    }

    if(unitArg is null || !unitArg.HasStringValue || string.IsNullOrWhiteSpace(unitArg.StringValue))
    {
      error = "expected second argument 'unit' to be a non-empty string.";
      return false;
    }

    scalar = valueArg.NumberValue;
    if(!TryParseUnit(quantityType, unitArg.StringValue, out unit, out var parseError))
    {
      error = parseError;
      return false;
    }

    return true;
  }

  public static bool TryCreateQuantity(
    QuantityType quantityType,
    AresValue? valueArg,
    AresValue? unitArg,
    out AresValue? quantityValue,
    out string? error)
  {
    quantityValue = null;

    if(!TryValidateConstructionArgs(quantityType, valueArg, unitArg, out var scalar, out var unit, out error))
    {
      return false;
    }

    var quantity = Quantity.From(scalar, unit);
    quantityValue = AresValueHelper.CreateQuantity(quantity.ToQuantityValue());
    return true;
  }

  public static string GetBaseUnitName(QuantityType quantityType)
  {
    return ResolveQuantityInfo(quantityType).BaseUnitInfo.Name;
  }

  private static QuantityInfo ResolveQuantityInfo(QuantityType quantityType)
  {
    var quantityName = quantityType.ToUnitsNetQuantityName();
    var quantityInfo = Quantity.Infos.FirstOrDefault(
      info => info.Name.Equals(quantityName, StringComparison.OrdinalIgnoreCase));

    return quantityInfo is null
      ? throw new InvalidOperationException($"No UnitsNet quantity mapping exists for QuantityType '{quantityType}'.")
      : quantityInfo;
  }
}
