using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using AresScript;
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
      var quantityTypeName = quantityType.ToString();
      var functionId = $"quantity::{quantityTypeName.ToLowerInvariant()}::from";
      var baseUnitName = QuantityUnitHelper.GetBaseUnitName(quantityType);

      symbols.Add(
        new AresSystemFunctionSymbol(
          functionId,
          "from",
          (args, _) =>
          {
            if(args.Count != 2)
            {
              throw new InvalidOperationException($"Quantity creation expects exactly 2 arguments but got {args.Count}.");
            }

            if(!QuantityUnitHelper.TryCreateQuantity(quantityType, args[0], args[1], out var quantityValue, out var error))
            {
              throw new InvalidOperationException($"Error creating quantity. {error}");
            }

            return Task.FromResult(quantityValue!);
          },
          BuildFromInputSchema(),
          AresSchemaBuilder.Entry(AresDataType.Quantity).WithQuantity(quantityType).Build(),
          Namespace: string.Empty,
          ParentName: $"Quantity.{quantityTypeName}")
        {
          Detail = $"Create a {quantityTypeName} quantity from a scalar and unit string.",
          Documentation = $"Create a {quantityTypeName} quantity. Example: Quantity.{quantityTypeName}.from(5, \"{baseUnitName}\")",
          StaticArgumentValidator = args =>
          {
            // Ignore the incorrect arg count and stuff, all those should be caught by the outer validation.
            // We should only concern ourselves with validating the actual units.
            if(args.Count < 2 || args[1] is not { HasStringValue: true } unitArg || string.IsNullOrWhiteSpace(unitArg.StringValue))
            {
              return new StaticArgValidation(true);
            }

            if(!QuantityUnitHelper.TryValidateConstructionArgs(quantityType, args.ElementAtOrDefault(0), unitArg, out _, out _, out var error))
            {
              return new StaticArgValidation(false, $"Function '{functionId}' {error}", 1);
              
            }

            return new StaticArgValidation(true);
          }
        });
    }

    return symbols.ToArray();
  }

  private static AresStructSchema BuildFromInputSchema()
  {
    return AresSchemaBuilder.Empty()
      .AddEntry("value", AresSchemaBuilder.Entry(AresDataType.Number).Build())
      .AddEntry("unit", AresSchemaBuilder.Entry(AresDataType.String).Build())
      .Build();
  }
}
