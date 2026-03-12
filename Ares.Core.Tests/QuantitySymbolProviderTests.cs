using Ares.Core.Scripting;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using AresScript;
using AresScript.Symbols;
using UnitsNet;
using UnitsNet.Units;

namespace Ares.Core.Tests;

[TestFixture]
public class QuantitySymbolProviderTests
{
  [Test]
  public void GetSymbols_Includes_Duration_From_Function()
  {
    var provider = new QuantitySymbolProvider();

    var function = provider
      .GetSymbols()
      .OfType<AresSystemFunctionSymbol>()
      .FirstOrDefault(symbol => symbol.ParentName == "Unit.Duration" && symbol.Name == "from");

    Assert.That(function, Is.Not.Null);
  }

  [Test]
  public async Task Duration_From_Creates_Quantity_Value()
  {
    var provider = new QuantitySymbolProvider();
    var function = provider
      .GetSymbols()
      .OfType<AresSystemFunctionSymbol>()
      .First(symbol => symbol.ParentName == "Unit.Duration" && symbol.Name == "from");

    var value = await function.Body(
      [AresValueHelper.CreateNumber(1), AresValueHelper.CreateString("s")],
      new ScriptExecutionControlToken(CancellationToken.None));

    Assert.That(value.KindCase, Is.EqualTo(AresValue.KindOneofCase.QuantityValue));
    var quantity = value.QuantityValue.ToUnitsNetQuantity();
    Assert.That(quantity.QuantityInfo.Name, Is.EqualTo(nameof(Duration)));
    Assert.That(quantity.As(DurationUnit.Second), Is.EqualTo(1).Within(0.0001));
  }
}
