using Ares.Core.Campaigns;
using Ares.Core.CustomCommands;
using Ares.Core.Device.Repos;
using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text.Json;

namespace Ares.Core.Tests.Campaigns;

internal class CampaignTemplateTransferServiceTests
{
  private SqliteConnection _connection;
  private IDbContextFactory<CoreDatabaseContext> _contextFactory;
  private CampaignTemplatePersistenceService _persistenceService;
  private CampaignTemplateTransferService _transferService;

  [SetUp]
  public async Task SetUp()
  {
    DatabaseRuntimeEnvironment.DatabaseProvider = "Sqlite";
    _connection = new SqliteConnection("Data Source=:memory:");
    await _connection.OpenAsync();
    var options = new DbContextOptionsBuilder<CoreDatabaseContext>().UseSqlite(_connection).Options;
    _contextFactory = new TestContextFactory(options);
    await using var context = await _contextFactory.CreateDbContextAsync();
    await context.Database.EnsureCreatedAsync();
    _persistenceService = new CampaignTemplatePersistenceService(_contextFactory);
    var customCommands = new Mock<ICustomCommandPersistenceService>();
    customCommands.Setup(service => service.GetCommandsAsync()).ReturnsAsync([]);
    var deviceRepo = new Mock<IAresDeviceRepo>();
    deviceRepo.Setup(repo => repo.GetAll()).Returns([]);
    _transferService = new CampaignTemplateTransferService(
      _persistenceService,
      customCommands.Object,
      deviceRepo.Object,
      _contextFactory);
  }

  [TearDown]
  public async Task TearDown()
    => await _connection.DisposeAsync();

  [Test]
  public async Task ExportAndImport_RoundTripsCurrentCampaignWithFreshGraphIds()
  {
    var original = CreateCampaign("Round Trip");
    await _persistenceService.AddAsync(original);
    var export = await _transferService.ExportAsync(original.UniqueId);

    var result = await _transferService.ImportAsync(export!.Json);
    var importedCommands = result.Template.ExperimentTemplate.StepTemplates.Single().CommandTemplates.OrderBy(command => command.Index).ToArray();
    var originalCommands = original.ExperimentTemplate.StepTemplates.Single().CommandTemplates.OrderBy(command => command.Index).ToArray();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(export.Template.UniqueId, Is.EqualTo(original.UniqueId));
      Assert.That(export.SuggestedFileName, Is.EqualTo("Round Trip.json"));
      Assert.That(result.Template.UniqueId, Is.Not.EqualTo(original.UniqueId));
      Assert.That(result.Template.Name, Is.EqualTo("Round Trip (Imported)"));
      Assert.That(importedCommands[0].UniqueId, Is.Not.EqualTo(originalCommands[0].UniqueId));
      Assert.That(importedCommands[0].CommandTypeCase, Is.EqualTo(CommandTemplate.CommandTypeOneofCase.DeviceCommand));
      Assert.That(importedCommands[1].CommandTypeCase, Is.EqualTo(CommandTemplate.CommandTypeOneofCase.SystemCommand));
      Assert.That(importedCommands[2].CommandTypeCase, Is.EqualTo(CommandTemplate.CommandTypeOneofCase.CustomCommandInvocation));
    }
  }

  [Test]
  public async Task Import_ConvertsLegacyMetadataAndParameters()
  {
    const string json =
      """
      {
        "uniqueId":"legacy-campaign",
        "name":"Legacy",
        "experimentTemplate":{
          "uniqueId":"legacy-experiment",
          "stepTemplates":[{
            "uniqueId":"legacy-step",
            "commandTemplates":[{
              "uniqueId":"legacy-command",
              "metadata":{"uniqueId":"legacy-metadata","name":"Dispense","deviceId":"pump-1","deviceType":"Pump"},
              "parameters":[{"uniqueId":"legacy-parameter","metadata":{"uniqueId":"legacy-parameter-metadata","name":"volume"},"literalSource":{"value":{"numberValue":5}}}],
              "index":0
            }]
          }]
        }
      }
      """;

    var result = await _transferService.ImportAsync(json);
    var command = result.Template.ExperimentTemplate.StepTemplates.Single().CommandTemplates.Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(command.CommandTypeCase, Is.EqualTo(CommandTemplate.CommandTypeOneofCase.DeviceCommand));
      Assert.That(command.DeviceCommand.Metadata.Name, Is.EqualTo("Dispense"));
      Assert.That(command.ArgumentBindings, Has.Count.EqualTo(1));
      Assert.That(command.ArgumentBindings.Single().LiteralSource.Value.NumberValue, Is.EqualTo(5));
    }
  }

  [Test]
  public async Task Import_ConvertsLegacyAresDeviceCommandToSystemCommand()
  {
    const string json =
      """
      {
        "name":"Legacy System Command",
        "experimentTemplate":{
          "stepTemplates":[{
            "commandTemplates":[{
              "uniqueId":"legacy-command",
              "metadata":{"name":"SleepForSeconds","deviceId":"ARES-CORE-DEVICE","deviceType":"ARES"},
              "parameters":[{"metadata":{"name":"Duration"},"literalSource":{"value":{"numberValue":3}}}],
              "index":0
            }]
          }]
        }
      }
      """;

    var result = await _transferService.ImportAsync(json);
    var command = result.Template.ExperimentTemplate.StepTemplates.Single().CommandTemplates.Single();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(command.CommandTypeCase, Is.EqualTo(CommandTemplate.CommandTypeOneofCase.SystemCommand));
      Assert.That(command.SystemCommand.Operation, Is.EqualTo(SystemOperation.SleepForSeconds));
      Assert.That(command.ArgumentBindings, Has.Count.EqualTo(1));
      Assert.That(command.ArgumentBindings.Single().LiteralSource.Value.NumberValue, Is.EqualTo(3));
    }
  }

  [Test]
  public void Import_RejectsMalformedOrStructurallyInvalidFiles()
  {
    Assert.ThrowsAsync<CampaignTemplateImportException>(() => _transferService.ImportAsync("not-json"));
    Assert.ThrowsAsync<CampaignTemplateImportException>(() => _transferService.ImportAsync("{\"name\":\"Missing experiment\"}"));
  }

  [Test]
  public async Task Import_PreservesMissingDeviceAndCustomReferencesWithWarnings()
  {
    var campaign = CreateCampaign("References");
    campaign.ExperimentTemplate.StepTemplates.Single().CommandTemplates.Add(new CommandTemplate
    {
      UniqueId = Guid.NewGuid().ToString(),
      CustomCommandInvocation = new CustomCommandInvocation { CustomCommandId = "missing-custom" }
    });
    await _persistenceService.AddAsync(campaign);
    var export = await _transferService.ExportAsync(campaign.UniqueId);

    var result = await _transferService.ImportAsync(export!.Json);

    Assert.That(result.Warnings, Has.Some.Contains("pump-1"));
    Assert.That(result.Warnings, Has.Some.Contains("missing-custom"));
    Assert.That(result.Template.ExperimentTemplate.StepTemplates.Single().CommandTemplates, Has.Count.EqualTo(4));
  }

  [Test]
  public async Task Import_UsesCanonicalLocalPlannerAndDropsUnavailableAllocation()
  {
    var localPlanner = new PlannerServiceInfo
    {
      UniqueId = Guid.NewGuid().ToString(),
      Name = "Planner",
      Type = "PlannerType",
      Version = "1.0",
      Capabilities = new PlannerServiceCapabilities()
    };
    await using(var context = await _contextFactory.CreateDbContextAsync())
    {
      context.PlannerInfos.Add(localPlanner);
      await context.SaveChangesAsync();
    }

    var campaign = CreateCampaign("Planning");
    var parameter = new ParameterMetadata { UniqueId = Guid.NewGuid().ToString(), Name = "temperature" };
    campaign.PlannableParameters.Add(parameter);
    campaign.PlannerAllocations.Add(new PlannerAllocation
    {
      UniqueId = Guid.NewGuid().ToString(),
      Parameter = parameter,
      Planner = new PlannerServiceInfo
      {
        UniqueId = "foreign-id",
        Name = "Planner",
        Type = "PlannerType",
        Version = "1.0",
        Capabilities = new PlannerServiceCapabilities()
      }
    });
    campaign.PlannerAllocations.Add(new PlannerAllocation
    {
      UniqueId = Guid.NewGuid().ToString(),
      Parameter = parameter,
      Planner = new PlannerServiceInfo { UniqueId = "missing", Name = "Missing", Capabilities = new PlannerServiceCapabilities() }
    });
    var json = JsonSerializer.Serialize(campaign, SerializerSettingsHelper.CreateCustomSerializationSettings());

    var result = await _transferService.ImportAsync(json);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(result.Template.PlannerAllocations, Has.Count.EqualTo(1));
      Assert.That(result.Template.PlannerAllocations.Single().Planner.UniqueId, Is.EqualTo(localPlanner.UniqueId));
      Assert.That(result.Warnings, Has.Some.Contains("Missing"));
    }
    await using var verificationContext = await _contextFactory.CreateDbContextAsync();
    Assert.That(await verificationContext.PlannerInfos.CountAsync(), Is.EqualTo(1));
  }

  private static CampaignTemplate CreateCampaign(string name)
  {
    var command = new CommandTemplate
    {
      UniqueId = Guid.NewGuid().ToString(),
      DeviceCommand = new DeviceCommand
      {
        Metadata = new CommandMetadata
        {
          UniqueId = Guid.NewGuid().ToString(),
          Name = "Dispense",
          DeviceId = "pump-1",
          DeviceType = "Pump",
          OutputMetadata = new OutputMetadata { UniqueId = Guid.NewGuid().ToString() }
        }
      }
    };
    var step = new StepTemplate { UniqueId = Guid.NewGuid().ToString() };
    step.CommandTemplates.AddRange([
      command,
      new CommandTemplate
      {
        UniqueId = Guid.NewGuid().ToString(),
        Index = 1,
        SystemCommand = new SystemCommand { Operation = SystemOperation.WaitForUser }
      },
      new CommandTemplate
      {
        UniqueId = Guid.NewGuid().ToString(),
        Index = 2,
        CustomCommandInvocation = new CustomCommandInvocation { CustomCommandId = Guid.NewGuid().ToString() }
      }
    ]);
    var experiment = new ExperimentTemplate { UniqueId = Guid.NewGuid().ToString() };
    experiment.StepTemplates.Add(step);
    return new CampaignTemplate
    {
      UniqueId = Guid.NewGuid().ToString(),
      Name = name,
      ExperimentTemplate = experiment
    };
  }

  private sealed class TestContextFactory(DbContextOptions<CoreDatabaseContext> options)
    : IDbContextFactory<CoreDatabaseContext>
  {
    public CoreDatabaseContext CreateDbContext()
      => new(options);
  }
}
