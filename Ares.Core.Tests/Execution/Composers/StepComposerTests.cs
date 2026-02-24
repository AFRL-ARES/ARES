using Ares.Core.Device.Repos;
using Ares.Core.Tests.Data.Device;
using Ares.Core.Execution.Executors;
using Ares.Core.Execution.Executors.Composers;
using Ares.Core.Notifications;
using Ares.Datamodel.Templates;
using Moq;
using System.Reflection;

namespace Ares.Core.Tests.Execution.Composers;

internal class StepComposerTests
{
  private StepTemplate _stepTemplate;
  private IAresDeviceRepo _deviceRepo;
  private INotifier _notifer;

  [SetUp]
  public void SetUp()
  {
    _deviceRepo = new AresDeviceRepo();
    _deviceRepo.Add(new TestDevice("Test Device", "TestDeviceId"));
    var commandTemplate1 = new CommandTemplate
    {
      Index = 0,
      UniqueId = Guid.NewGuid().ToString(),
      Metadata = new CommandMetadata { UniqueId = Guid.NewGuid().ToString(), DeviceId = "TestDeviceId" }
    };

    var commandTemplate2 = new CommandTemplate
    {
      Index = 1,
      UniqueId = Guid.NewGuid().ToString(),
      Metadata = new CommandMetadata { UniqueId = Guid.NewGuid().ToString(), DeviceId = "TestDeviceId" }
    };

    var commandTemplate3 = new CommandTemplate
    {
      Index = 2,
      UniqueId = Guid.NewGuid().ToString(),
      Metadata = new CommandMetadata { UniqueId = Guid.NewGuid().ToString(), DeviceId = "TestDeviceId" }
    };

    var commandTemplate4 = new CommandTemplate
    {
      Index = 3,
      UniqueId = Guid.NewGuid().ToString(),
      Metadata = new CommandMetadata { UniqueId = Guid.NewGuid().ToString(), DeviceId = "TestDeviceId" }
    };

    var stepTemplate = new StepTemplate
    { Index = 0, UniqueId = Guid.NewGuid().ToString() };

    stepTemplate.CommandTemplates.Add(commandTemplate3);
    stepTemplate.CommandTemplates.Add(commandTemplate1);
    stepTemplate.CommandTemplates.Add(commandTemplate4);
    stepTemplate.CommandTemplates.Add(commandTemplate2);

    _stepTemplate = stepTemplate;
  }

  [OneTimeSetUp]
  public void OneTimeSetUp()
  {
    _notifer = new Mock<INotifier>().Object;
  }

  [Test]
  public void StepComposer_Composes_CommandTemplates_Correctly()
  {
    var stepComposer = new StepComposer(_deviceRepo, _notifer);
    var stepExecutor = stepComposer.Compose(_stepTemplate);
    var templates = stepExecutor.CommandExecutors.Select(executor => typeof(CommandExecutor).GetProperty("Template", BindingFlags.Public | BindingFlags.Instance).GetValue(executor)).OfType<CommandTemplate>();
    Assert.That(templates.Select((template, i) => template.Index == i), Is.All.True);
  }

  [Test]
  public void StepComposer_Composes_Parallel_Template()
  {
    _stepTemplate.IsParallel = true;
    var stepComposer = new StepComposer(_deviceRepo, _notifer);
    var stepExecutor = stepComposer.Compose(_stepTemplate);
    Assert.That(stepExecutor, Is.TypeOf<ParallelStepExecutor>());
  }

  [Test]
  public void StepComposer_Composes_Sequential_Template()
  {
    _stepTemplate.IsParallel = false;
    var stepComposer = new StepComposer(_deviceRepo, _notifer);
    var stepExecutor = stepComposer.Compose(_stepTemplate);
    Assert.That(stepExecutor, Is.TypeOf<SequentialStepExecutor>());
  }
}
