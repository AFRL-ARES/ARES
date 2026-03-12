using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using AresScript.Symbols;
using UnitsNet;

namespace Ares.Core.Scripting;

public class QuantitySymbolProvider : ISymbolProvider
{
  public IScriptSymbol[] GetSymbols()
  {
    var symbols = new List<IScriptSymbol>();

    foreach(var quantityType in Enum.GetValues<QuantityType>().Where(type => type != QuantityType.Unspecified))
    {
      var quantityInfo = ResolveQuantityInfo(quantityType);
      var quantityTypeName = quantityType.ToString();
      var functionId = $"unit::{quantityTypeName.ToLowerInvariant()}::from";

      symbols.Add(
        new AresSystemFunctionSymbol(
          functionId,
          "from",
          (args, _) =>
          {
            if(args.Count != 2)
            {
              throw new InvalidOperationException($"Function '{functionId}' expected exactly 2 arguments but got {args.Count}.");
            }

            if(!args[0].HasNumberValue)
            {
              throw new InvalidOperationException($"Function '{functionId}' expected first argument 'value' to be a number.");
            }

            if(!args[1].HasStringValue || string.IsNullOrWhiteSpace(args[1].StringValue))
            {
              throw new InvalidOperationException($"Function '{functionId}' expected second argument 'unit' to be a non-empty string.");
            }

            var unit = ParseUnit(quantityInfo, args[1].StringValue, quantityType);
            var quantity = Quantity.From(args[0].NumberValue, unit);
            return Task.FromResult(AresValueHelper.CreateQuantity(quantity.ToQuantityValue()));
          },
          BuildFromInputSchema(),
          AresSchemaBuilder.Entry(AresDataType.Quantity).WithQuantity(quantityType).Build(),
          Namespace: string.Empty,
          ParentName: $"Unit.{quantityTypeName}")
        {
          Detail = $"Create a {quantityTypeName} quantity from a scalar and unit string.",
          Documentation = $"Create a {quantityTypeName} quantity. Example: Unit.{quantityTypeName}.from(5, \"{quantityInfo.BaseUnitInfo.Name}\")"
        });
    }

    return symbols.ToArray();
  }

  private static AresDataSchema BuildFromInputSchema()
  {
    return AresSchemaBuilder.Empty()
      .AddEntry("value", AresSchemaBuilder.Entry(AresDataType.Number).Build())
      .AddEntry("unit", AresSchemaBuilder.Entry(AresDataType.String).Build())
      .Build();
  }

  private static QuantityInfo ResolveQuantityInfo(QuantityType quantityType)
  {
    var quantityName = quantityType.ToUnitsNetQuantityName();
    var quantityInfo = Quantity.Infos.FirstOrDefault(
      info => info.Name.Equals(quantityName, StringComparison.OrdinalIgnoreCase));

    return quantityInfo is null
      ? throw new InvalidOperationException($"No UnitsNet quantity info exists for QuantityType '{quantityType}'.")
      : quantityInfo;
  }

  private static Enum ParseUnit(QuantityInfo quantityInfo, string unitText, QuantityType quantityType)
  {
    var enumUnit = quantityInfo.UnitInfos
      .Select(unitInfo => unitInfo.Value)
      .FirstOrDefault(u => u.ToString().Equals(unitText, StringComparison.OrdinalIgnoreCase));

    if(enumUnit is not null)
    {
      return enumUnit;
    }

    if(UnitParser.Default.TryParse(unitText, quantityInfo.UnitType, out Enum? parsedUnit) && parsedUnit is not null)
    {
      return parsedUnit;
    }

    throw new InvalidOperationException(
      $"Unit '{unitText}' is not valid for QuantityType '{quantityType}'. Use a valid {quantityInfo.Name} unit.");
  }
}
