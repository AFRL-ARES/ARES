using Ares.Core.Campaigns;
using Ares.Datamodel;
using Ares.Datamodel.Templates;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Tests.Campaigns;

internal class CampaignTemplatePersistenceServiceTests
{
  private SqliteConnection _connection;
  private IDbContextFactory<CoreDatabaseContext> _contextFactory;
  private CampaignTemplatePersistenceService _service;

  [SetUp]
  public async Task SetUp()
  {
    DatabaseRuntimeEnvironment.DatabaseProvider = "Sqlite";
    _connection = new SqliteConnection("Data Source=:memory:");
    await _connection.OpenAsync();
    var options = new DbContextOptionsBuilder<CoreDatabaseContext>()
      .UseSqlite(_connection)
      .Options;
    _contextFactory = new TestContextFactory(options);
    await using var context = await _contextFactory.CreateDbContextAsync();
    await context.Database.EnsureCreatedAsync();
    _service = new CampaignTemplatePersistenceService(_contextFactory);
  }

  [TearDown]
  public async Task TearDown()
    => await _connection.DisposeAsync();

  [Test]
  public async Task AddAndGet_RoundTripsAllCommandTypes()
  {
    var campaign = CreateCampaign("Campaign A");
    await _service.AddAsync(campaign);

    var loaded = await _service.GetByIdAsync(campaign.UniqueId);
    var commands = loaded!.ExperimentTemplate.StepTemplates.Single().CommandTemplates.OrderBy(command => command.Index).ToArray();

    using(Assert.EnterMultipleScope())
    {
      Assert.That(commands[0].CommandTypeCase, Is.EqualTo(CommandTemplate.CommandTypeOneofCase.DeviceCommand));
      Assert.That(commands[0].DeviceCommand.Metadata.Name, Is.EqualTo("Dispense"));
      Assert.That(commands[0].ArgumentBindings, Has.Count.EqualTo(1));
      Assert.That(commands[0].OutputVarName, Is.EqualTo("dispensed"));
      Assert.That(commands[1].CommandTypeCase, Is.EqualTo(CommandTemplate.CommandTypeOneofCase.SystemCommand));
      Assert.That(commands[2].CommandTypeCase, Is.EqualTo(CommandTemplate.CommandTypeOneofCase.CustomCommandInvocation));
    }

    var summaries = await _service.GetSummariesAsync();
    Assert.That(summaries.Single().CampaignName, Is.EqualTo("Campaign A"));
    Assert.That(await _service.ExistsByIdAsync(campaign.UniqueId), Is.True);
    Assert.That(await _service.ExistsByNameAsync("Campaign A"), Is.True);
    Assert.That((await _service.GetByNameAsync("Campaign A"))!.UniqueId, Is.EqualTo(campaign.UniqueId));
  }

  [Test]
  public async Task Replace_RemovesOldGraphAndKeepsStableCampaignId()
  {
    var campaign = CreateCampaign("Original");
    await _service.AddAsync(campaign);
    var oldCommandIds = campaign.ExperimentTemplate.StepTemplates.Single().CommandTemplates.Select(command => command.UniqueId).ToArray();
    var replacement = CreateCampaign("Replacement");
    replacement.UniqueId = campaign.UniqueId;
    replacement.ExperimentTemplate.StepTemplates.Single().CommandTemplates.RemoveAt(0);

    var replaced = await _service.ReplaceAsync(replacement);
    var loaded = await _service.GetByIdAsync(campaign.UniqueId);

    using(Assert.EnterMultipleScope())
    {
      Assert.That(replaced, Is.True);
      Assert.That(loaded!.UniqueId, Is.EqualTo(campaign.UniqueId));
      Assert.That(loaded.Name, Is.EqualTo("Replacement"));
      Assert.That(loaded.ExperimentTemplate.StepTemplates.Single().CommandTemplates, Has.Count.EqualTo(2));
    }
    await using var context = await _contextFactory.CreateDbContextAsync();
    Assert.That(await context.CommandTemplates.IgnoreAutoIncludes().CountAsync(command => oldCommandIds.Contains(command.UniqueId)), Is.Zero);
  }

  [Test]
  public async Task Delete_RemovesCompleteCampaignGraph()
  {
    var campaign = CreateCampaign("Delete me");
    await _service.AddAsync(campaign);

    Assert.That(await _service.DeleteAsync(campaign.UniqueId), Is.True);
    Assert.That(await _service.GetByIdAsync(campaign.UniqueId), Is.Null);

    await using var context = await _contextFactory.CreateDbContextAsync();
    using(Assert.EnterMultipleScope())
    {
      Assert.That(await context.ExperimentTemplates.IgnoreAutoIncludes().CountAsync(), Is.Zero);
      Assert.That(await context.StepTemplates.IgnoreAutoIncludes().CountAsync(), Is.Zero);
      Assert.That(await context.CommandTemplates.IgnoreAutoIncludes().CountAsync(), Is.Zero);
    }
  }

  [Test]
  public async Task Add_RejectsDuplicateCampaignName()
  {
    await _service.AddAsync(CreateCampaign("Duplicate"));

    Assert.ThrowsAsync<DbUpdateException>(() => _service.AddAsync(CreateCampaign("Duplicate")));
  }

  private static CampaignTemplate CreateCampaign(string name)
  {
    var deviceCommand = new CommandTemplate
    {
      UniqueId = Guid.NewGuid().ToString(),
      Index = 0,
      OutputVarName = "dispensed",
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
    deviceCommand.ArgumentBindings.Add(new Parameter
    {
      UniqueId = Guid.NewGuid().ToString(),
      Metadata = new ParameterMetadata { Name = "volume" },
      LiteralSource = new LiteralParameterSource { Value = new AresValue { NumberValue = 5 } }
    });
    var systemCommand = new CommandTemplate
    {
      UniqueId = Guid.NewGuid().ToString(),
      Index = 1,
      SystemCommand = new SystemCommand { Operation = SystemOperation.WaitForUser }
    };
    var customCommand = new CommandTemplate
    {
      UniqueId = Guid.NewGuid().ToString(),
      Index = 2,
      CustomCommandInvocation = new CustomCommandInvocation { CustomCommandId = Guid.NewGuid().ToString() }
    };
    var step = new StepTemplate { UniqueId = Guid.NewGuid().ToString(), Name = "Step" };
    step.CommandTemplates.AddRange([deviceCommand, systemCommand, customCommand]);
    var experiment = new ExperimentTemplate { UniqueId = Guid.NewGuid().ToString(), Name = "Experiment" };
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
