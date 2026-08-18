using Ares.Core.Device.Repos;
using Ares.Core.Tests.Data.Device;
using Ares.Core.Execution.Executors;
using Ares.Core.Execution.Executors.Composers;
using Ares.Core.Notifications;
using Ares.Datamodel.Templates;
using Moq;
using System.Reflection;
using Ares.Core.Settings;
using Ares.Core.CustomCommands;
using Ares.Core.Scripting;

namespace Ares.Core.Tests.Execution.Composers;

internal class StepComposerTests
{
  private StepTemplate _stepTemplate;
  private AresDeviceRepo _deviceRepo;
  private INotifier _notifer;
  private ISystemSettingsManager _settingsManager;
  private ICommandDisplayNameResolver _commandDisplayNameResolver;

  [SetUp]
  public void SetUp()
  {
    _deviceRepo = new AresDeviceRepo();
    _deviceRepo.AddOrUpdate(new TestDevice("Test Device", "TestDeviceId"));
    var commandTemplate1 = new CommandTemplate
    {
      Index = 0,
      UniqueId = Guid.NewGuid().ToString(),
      DeviceCommand = new DeviceCommand { Metadata = new CommandMetadata { UniqueId = Guid.NewGuid().ToString(), DeviceId = "TestDeviceId" } }
    };

    var commandTemplate2 = new CommandTemplate
    {
      Index = 1,
      UniqueId = Guid.NewGuid().ToString(),
      DeviceCommand = new DeviceCommand { Metadata = new CommandMetadata { UniqueId = Guid.NewGuid().ToString(), DeviceId = "TestDeviceId" } }
    };

    var commandTemplate3 = new CommandTemplate
    {
      Index = 2,
      UniqueId = Guid.NewGuid().ToString(),
      DeviceCommand = new DeviceCommand { Metadata = new CommandMetadata { UniqueId = Guid.NewGuid().ToString(), DeviceId = "TestDeviceId" } }
    };

    var commandTemplate4 = new CommandTemplate
    {
      Index = 3,
      UniqueId = Guid.NewGuid().ToString(),
      DeviceCommand = new DeviceCommand { Metadata = new CommandMetadata { UniqueId = Guid.NewGuid().ToString(), DeviceId = "TestDeviceId" } }
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
    _settingsManager = new Mock<ISystemSettingsManager>().Object;
    var resolver = new Mock<ICommandDisplayNameResolver>();
    resolver.Setup(value => value.Resolve(It.IsAny<CommandTemplate>())).Returns("Test command");
    _commandDisplayNameResolver = resolver.Object;
  }

  [TearDown]
  public void Dispose()
  {
    _deviceRepo.Dispose();
  }

  [Test]
  public void StepComposer_Composes_CommandTemplates_Correctly()
  {
    var stepComposer = new StepComposer(_deviceRepo, _notifer, _settingsManager, _commandDisplayNameResolver);
    var stepExecutor = stepComposer.Compose(_stepTemplate);
    var templates = stepExecutor.CommandExecutors.Select(executor => typeof(CommandExecutor).GetProperty("Template", BindingFlags.Public | BindingFlags.Instance).GetValue(executor)).OfType<CommandTemplate>();
    Assert.That(templates.Select((template, i) => template.Index == i), Is.All.True);
  }

  [Test]
  public void StepComposer_Composes_Parallel_Template()
  {
    _stepTemplate.IsParallel = true;
    var stepComposer = new StepComposer(_deviceRepo, _notifer, _settingsManager, _commandDisplayNameResolver);
    var stepExecutor = stepComposer.Compose(_stepTemplate);
    Assert.That(stepExecutor, Is.TypeOf<ParallelStepExecutor>());
  }

  [Test]
  public void StepComposer_Composes_Sequential_Template()
  {
    _stepTemplate.IsParallel = false;
    var stepComposer = new StepComposer(_deviceRepo, _notifer, _settingsManager, _commandDisplayNameResolver);
    var stepExecutor = stepComposer.Compose(_stepTemplate);
    Assert.That(stepExecutor, Is.TypeOf<SequentialStepExecutor>());
  }

  [Test]
  public void StepComposer_UsesResolvedCustomCommandNameBeforeExecution()
  {
    var template = new StepTemplate();
    template.CommandTemplates.Add(new CommandTemplate
    {
      UniqueId = Guid.NewGuid().ToString(),
      CustomCommandInvocation = new CustomCommandInvocation { CustomCommandId = Guid.NewGuid().ToString() }
    });
    var resolver = new Mock<ICommandDisplayNameResolver>();
    resolver.Setup(value => value.Resolve(It.IsAny<CommandTemplate>())).Returns("Measure Temperature");
    var customCommandExecutor = new CustomCommandExecutor(
      new Mock<ICustomCommandPersistenceService>().Object,
      new BaseEnvironmentBuilder([]));
    var stepComposer = new StepComposer(_deviceRepo, _notifer, _settingsManager, resolver.Object, customCommandExecutor);

    var stepExecutor = stepComposer.Compose(template);

    Assert.That(stepExecutor.CommandExecutors.Single().Status.CommandName, Is.EqualTo("Measure Temperature"));
  }
}
