using System.Reflection;
using Ares.Messaging;
using Ares.Messaging.Analyzing;
using Ares.Messaging.Device;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core;

public class CoreDatabaseContext : DbContext
{
  public CoreDatabaseContext(DbContextOptions options) : base(options)
  {
  }

  public DbSet<CampaignTemplate> CampaignTemplates => Set<CampaignTemplate>();
  public DbSet<Project> Projects => Set<Project>();
  public DbSet<StepTemplate> StepTemplates => Set<StepTemplate>();
  public DbSet<ExperimentTemplate> ExperimentTemplates => Set<ExperimentTemplate>();
  public DbSet<CommandTemplate> CommandTemplates => Set<CommandTemplate>();
  public DbSet<PlannerTransaction> PlannerTransactions => Set<PlannerTransaction>();
  public DbSet<CampaignExecutionSummary> CampaignExecutionSummaries => Set<CampaignExecutionSummary>();
  public DbSet<DeviceConfig> DeviceConfigs => Set<DeviceConfig>();
  public DbSet<AnalyzerConfig> Analyzers => Set<AnalyzerConfig>();
  public DbSet<AnalyzerInfo> AnalyzerInfos => Set<AnalyzerInfo>();
  public DbSet<AnalyzerSettings> AnalyzerSettings => Set<AnalyzerSettings>();
  public DbSet<PlannerAdapterInfo> Planners => Set<PlannerAdapterInfo>();
  public DbSet<AresCampaignTag> CampaignTags => Set<AresCampaignTag>();
  public DbSet<Parameter> Parameters => Set<Parameter>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    var assembly = Assembly.GetAssembly(typeof(CoreDatabaseContext));
    if(assembly is null)
      return;

    modelBuilder.ApplyConfigurationsFromAssembly(assembly);
    base.OnModelCreating(modelBuilder);
  }
}
