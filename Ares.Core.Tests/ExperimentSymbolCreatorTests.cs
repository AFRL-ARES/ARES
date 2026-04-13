using Ares.Core.Scripting;
using Ares.Datamodel.Extensions;
using AresScript;
using AresScript.Symbols;

namespace Ares.Core.Tests;

[TestFixture]
public class ExperimentSymbolCreatorTests
{
  [Test]
  public void CreateFail_Throws_With_Provided_Message()
  {
    var fail = ExperimentSymbolCreator.CreateFail();

    var ex = Assert.ThrowsAsync<InvalidOperationException>(() => fail.Body(
      [AresValueHelper.CreateString("Laser interlock open")],
      new ScriptExecutionControlToken(CancellationToken.None)));

    Assert.That(ex?.Message, Is.EqualTo("Laser interlock open"));
  }

  [Test]
  public void CreateFail_Rejects_Missing_Message()
  {
    var fail = ExperimentSymbolCreator.CreateFail();

    var ex = Assert.ThrowsAsync<InvalidOperationException>(() => fail.Body(
      [],
      new ScriptExecutionControlToken(CancellationToken.None)));

    Assert.That(ex?.Message, Does.Contain("exactly 1 argument"));
  }
}
