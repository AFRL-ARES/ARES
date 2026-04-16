using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using AresScript.Environment;
using AresScript.Symbols;
using NUnit.Framework;

namespace AresScript.Tests;

[TestFixture]
public class AresScriptEnvironmentTests
{
  [Test]
  public void ScopeSpecificGetters_ReturnOnlySymbolsFromRequestedScope()
  {
    var environment = new AresScriptEnvironment();
    environment.AssignVariable("globalValue", AresValueHelper.CreateNumber(1));
    environment.AssignFunction("globalFunc", new AresScriptFunction("globalFunc", [], null!, AresDataType.Any));

    environment.EnterScope("experiment");
    environment.AssignVariable("experimentValue", AresValueHelper.CreateNumber(2));
    environment.AssignFunction("experimentFunc", new AresScriptFunction("experimentFunc", [], null!, AresDataType.Any));

    environment.EnterScope("loop");
    environment.AssignVariable("loopValue", AresValueHelper.CreateNumber(3));
    environment.AssignFunction("loopFunc", new AresScriptFunction("loopFunc", [], null!, AresDataType.Any));

    var experimentSymbols = environment.GetAllUserSymbols("experiment");
    var experimentFunctions = environment.GetAllUserFunctions("experiment");
    var experimentVariableNames = environment.GetAllUserVariableNames("experiment");

    Assert.Multiple(() =>
    {
      Assert.That(experimentSymbols.Select(symbol => symbol.Name), Is.EquivalentTo(["experimentValue", "experimentFunc"]));
      Assert.That(experimentFunctions.Select(function => function.Name), Is.EquivalentTo(["experimentFunc"]));
      Assert.That(experimentVariableNames, Is.EquivalentTo(["experimentValue"]));
    });
  }

  [Test]
  public void ScopeSpecificTryGetters_IgnoreMatchingSymbolsInOtherScopes()
  {
    var environment = new AresScriptEnvironment();
    environment.AssignVariable("shared", AresValueHelper.CreateNumber(1));
    environment.AssignFunction("shared", new AresScriptFunction("shared", [], null!, AresDataType.Any));

    environment.EnterScope("experiment");
    environment.AssignVariable("shared", AresValueHelper.CreateNumber(2));
    environment.AssignFunction("shared", new AresScriptFunction("shared", [], null!, AresDataType.String));

    environment.EnterScope("loop");

    Assert.Multiple(() =>
    {
      Assert.That(environment.TryGetUserValueSymbol("shared", "experiment", out var experimentValue), Is.True);
      Assert.That(experimentValue?.Value.NumberValue, Is.EqualTo(2));

      Assert.That(environment.TryGetUserFunction("shared", "experiment", out var experimentFunction), Is.True);
      Assert.That(experimentFunction?.ReturnType, Is.EqualTo(AresDataType.String));

      Assert.That(environment.TryGetUserValueSymbol("shared", "loop", out _), Is.False);
      Assert.That(environment.TryGetUserFunction("shared", "loop", out _), Is.False);
    });
  }
}
