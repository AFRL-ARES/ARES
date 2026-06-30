using Ares.Core.Device.Repos;
using Ares.Core.Execution.ControlTokens;
using Ares.Core.Execution.Executors;
using Ares.Core.Execution.Executors.Composers;
using Ares.Core.Execution.Interaction;
using Ares.Core.Notifications;
using Ares.Core.Settings;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Templates;
using Ares.Device;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Ares.Core.Tests.Execution;

internal class CommandVariableResolutionTests
{
  private ISystemSettingsManager _systemSettingsManager;
  private IDbContextFactory<CoreDatabaseContext> _dbContextFactory;
  private IUserInteractionBroker _userInteractionBroker;
  
  [OneTimeSetUp]
  public void OneTimeSetUp()
  {
    _dbContextFactory = new Mock<IDbContextFactory<CoreDatabaseContext>>().Object;
    _systemSettingsManager = new Mock<ISystemSettingsManager>().Object;
    _userInteractionBroker = new Mock<IUserInteractionBroker>().Object;
  }

  [Test]
  public async Task SequentialStepExecutor_ResolvesVariableParameterFromEarlierCommandOutput()
  {
    var device = new RecordingDevice("device-id");
    using var deviceRepo = new AresDeviceRepo();
    deviceRepo.AddOrUpdate(device);

    var stepTemplate = new StepTemplate();
    stepTemplate.CommandTemplates.Add(CreateSourceCommand());
    stepTemplate.CommandTemplates.Add(CreateConsumerCommand("sourceResult"));

    var stepComposer = new StepComposer(deviceRepo, new Mock<INotifier>().Object, _systemSettingsManager, _userInteractionBroker);
    var stepExecutor = stepComposer.Compose(stepTemplate);

    using var tokenSource = new ExecutionControlTokenSource();
    var summary = await stepExecutor.Execute(tokenSource.Token);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(summary.CommandSummaries, Has.Count.EqualTo(2));
      Assert.That(device.LastConsumerArguments, Is.Not.Null);
    }
    Assert.That(device.LastConsumerArguments.Single().ArgValue.NumberValue, Is.EqualTo(42));
  }

  [Test]
  public async Task CommandExecutor_FailsVariableParameterWhenVariableIsMissing()
  {
    var executed = false;
    var template = CreateConsumerCommand("missingResult");
    var executor = new CommandExecutor(
      _ =>
      {
        executed = true;
        return Task.FromResult(new CommandResult { Success = true });
      },
      template,
      new Mock<INotifier>().Object,
      _systemSettingsManager);

    using var tokenSource = new ExecutionControlTokenSource();
    var summary = await executor.Execute(tokenSource.Token);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(executed, Is.False);
      Assert.That(summary.Result.Success, Is.False);
      Assert.That(summary.Result.Error, Does.Contain("missingResult"));
    }
  }

  private static CommandTemplate CreateSourceCommand()
    => new()
    {
      UniqueId = Guid.NewGuid().ToString(),
      Index = 0,
      OutputVarName = "sourceResult",
      Metadata = new CommandMetadata
      {
        DeviceId = "device-id",
        Name = "source",
        OutputMetadata = new OutputMetadata
        {
          DataSchema = new AresValueSchema { Type = AresDataType.Number }
        }
      }
    };

  private static CommandTemplate CreateConsumerCommand(string variableArgument)
  {
    var template = new CommandTemplate
    {
      UniqueId = Guid.NewGuid().ToString(),
      Index = 1,
      Metadata = new CommandMetadata
      {
        DeviceId = "device-id",
        Name = "consumer"
      }
    };

    template.Parameters.Add(new Parameter
    {
      UniqueId = Guid.NewGuid().ToString(),
      CommandVariableSource = new CommandVariableParameterSource { VariableArgument = variableArgument },
      Metadata = new ParameterMetadata
      {
        Name = "input",
        Schema = new AresValueSchema { Type = AresDataType.Number }
      }
    });

    return template;
  }

  private sealed class RecordingDevice : AresDevice
  {
    private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());

    public RecordingDevice(string id) : base(new DeviceConnectionInfo { DeviceId = id, DeviceName = "Recording Device" })
    {
    }

    public List<DeviceCommandArgument> LastConsumerArguments { get; private set; }

    public override Task<bool> Activate(CancellationToken ct) => Task.FromResult(true);

    public override Task EnterSafeMode(CancellationToken ct) => Task.CompletedTask;

    public override Task<AresStruct> GetState() => Task.FromResult(new AresStruct());

    public override Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> parameters, CancellationToken token)
    {
      if(command == "consumer")
      {
        LastConsumerArguments = parameters;
        return Task.FromResult(new CommandResult { Success = true });
      }

      return Task.FromResult(new CommandResult
      {
        Success = true,
        Result = new AresValue { NumberValue = 42 }
      });
    }

    public override Task UpdateSettings(AresStruct settings) => Task.CompletedTask;

    public override Task<AresStruct> GetSettings() => Task.FromResult(new AresStruct());

    protected override Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
      => Task.FromResult<List<DeviceCommandDescriptor>>([]);

    public override IObservable<AresStruct> StateStream => _stateSubject.AsObservable();
  }
}
