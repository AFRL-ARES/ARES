using Ares.Core.Execution.Executors;
using Ares.Datamodel;
using Ares.Datamodel.Templates;

namespace Ares.Core.Tests.Execution;

public class SystemOperationCatalogTests
{
  [Test]
  public void Definitions_ContainEverySupportedOperation()
  {
    var expectedOperations = Enum.GetValues<SystemOperation>()
      .Where(operation => operation != SystemOperation.Undefined)
      .ToArray();

    Assert.That(SystemOperationCatalog.Definitions.Select(definition => definition.Operation), Is.EquivalentTo(expectedOperations));
  }

  [TestCase(SystemOperation.SleepForMilliseconds)]
  [TestCase(SystemOperation.SleepForSeconds)]
  [TestCase(SystemOperation.SleepForMinutes)]
  public void SleepDefinitions_HaveDurationInputAndNoOutput(SystemOperation operation)
  {
    var definition = SystemOperationCatalog.Find(operation);

    Assert.Multiple(() =>
    {
      Assert.That(definition, Is.Not.Null);
      Assert.That(definition!.Parameters, Has.Count.EqualTo(1));
      Assert.That(definition.Parameters[0].Name, Is.EqualTo("Duration"));
      Assert.That(definition.Parameters[0].Schema.Type, Is.EqualTo(AresDataType.Number));
      Assert.That(definition.OutputSchema, Is.Null);
    });
  }

  [TestCase(SystemOperation.WaitForUser)]
  [TestCase(SystemOperation.WaitForUserInput)]
  public void WaitDefinitions_HaveNoInputsOrOutput(SystemOperation operation)
  {
    var definition = SystemOperationCatalog.Find(operation);

    Assert.Multiple(() =>
    {
      Assert.That(definition, Is.Not.Null);
      Assert.That(definition!.Parameters, Is.Empty);
      Assert.That(definition.OutputSchema, Is.Null);
    });
  }

  [Test]
  public void GetTimestampDefinition_HasTimestampOutput()
  {
    var definition = SystemOperationCatalog.Find(SystemOperation.GetTimestamp);

    Assert.Multiple(() =>
    {
      Assert.That(definition, Is.Not.Null);
      Assert.That(definition!.Parameters, Is.Empty);
      Assert.That(definition.OutputSchema?.Type, Is.EqualTo(AresDataType.Timestamp));
    });
  }

  [Test]
  public void CalculateAverageDefinition_HasNumberArrayInputAndNumberOutput()
  {
    var definition = SystemOperationCatalog.Find(SystemOperation.CalculateAverage);

    Assert.Multiple(() =>
    {
      Assert.That(definition, Is.Not.Null);
      Assert.That(definition!.Parameters, Has.Count.EqualTo(1));
      Assert.That(definition.Parameters[0].Schema.Type, Is.EqualTo(AresDataType.NumberArray));
      Assert.That(definition.OutputSchema?.Type, Is.EqualTo(AresDataType.Number));
    });
  }
}
