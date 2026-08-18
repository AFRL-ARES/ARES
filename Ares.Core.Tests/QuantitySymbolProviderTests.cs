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
  public async Task GetSymbols_Includes_Duration_From_Function()
  {
    var provider = new QuantitySymbolProvider();
    var symbols = await provider.GetSymbols();

    var function = symbols
      .OfType<AresSystemFunctionSymbol>()
      .FirstOrDefault(symbol => symbol.ParentName == "Quantity.Duration" && symbol.Name == "from");

    Assert.That(function, Is.Not.Null);
  }

  [Test]
  public async Task Duration_From_Creates_Quantity_Value()
  {
    var provider = new QuantitySymbolProvider();
    var symbols = await provider.GetSymbols();

    var function = symbols
      .OfType<AresSystemFunctionSymbol>()
      .First(symbol => symbol.ParentName == "Quantity.Duration" && symbol.Name == "from");

    var value = await function.Body(
      [AresValueHelper.CreateNumber(1), AresValueHelper.CreateString("s")],
      new ScriptExecutionControlToken(CancellationToken.None));

    Assert.That(value.KindCase, Is.EqualTo(AresValue.KindOneofCase.QuantityValue));
    var quantity = value.QuantityValue.ToUnitsNetQuantity();
    Assert.That(quantity.QuantityInfo.Name, Is.EqualTo(nameof(Duration)));
    Assert.That(quantity.As(DurationUnit.Second), Is.EqualTo(1).Within(0.0001));
  }

  [Test]
  public async Task Duration_From_Rejects_Invalid_Unit_At_Runtime()
  {
    var provider = new QuantitySymbolProvider();
    var symbols = await provider.GetSymbols();

    var function = symbols
      .OfType<AresSystemFunctionSymbol>()
      .First(symbol => symbol.ParentName == "Quantity.Duration" && symbol.Name == "from");

    var ex = Assert.ThrowsAsync<InvalidOperationException>(() => function.Body(
      [AresValueHelper.CreateNumber(1), AresValueHelper.CreateString("kg")],
      new ScriptExecutionControlToken(CancellationToken.None)));

    Assert.That(ex?.Message, Does.Contain("not valid for quantity type 'Duration'"));
  }

  [Test]
  public async Task Duration_From_StaticArgumentValidator_Rejects_Invalid_Unit()
  {
    var provider = new QuantitySymbolProvider();
    var symbols = await provider.GetSymbols();

    var function = symbols
      .OfType<AresSystemFunctionSymbol>()
      .First(symbol => symbol.ParentName == "Quantity.Duration" && symbol.Name == "from");

    var error = function.StaticArgumentValidator?.Invoke(
      [AresValueHelper.CreateNumber(1), AresValueHelper.CreateString("kg")]);

    Assert.That(error?.Error, Does.Contain("not valid for quantity type 'Duration'"));
  }
}
