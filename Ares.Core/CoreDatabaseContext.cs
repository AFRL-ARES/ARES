using System.Reflection;
using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Automation;
using Ares.Datamodel.Device;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;
using Ares.Datamodel.Visualizing.Local;
using Ares.Services;
using Google.Protobuf.WellKnownTypes;
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
  public DbSet<CampaignExecutionSummary> CampaignExecutionSummaries => Set<CampaignExecutionSummary>();
  public DbSet<DeviceConfig> DeviceConfigs => Set<DeviceConfig>();
  public DbSet<RemoteDeviceConfig> RemoteDeviceConfigs => Set<RemoteDeviceConfig>();
  public DbSet<DeviceSettings> DeviceSettings => Set<DeviceSettings>();
  public DbSet<DriverInfo> DeviceDrivers => Set<DriverInfo>();
  public DbSet<DeviceInfo> DeviceInfos => Set<DeviceInfo>();
  public DbSet<AnalyzerConfig> Analyzers => Set<AnalyzerConfig>();
  public DbSet<AnalyzerInfo> AnalyzerInfos => Set<AnalyzerInfo>();
  public DbSet<AnalyzerSettings> AnalyzerSettings => Set<AnalyzerSettings>();
  public DbSet<PlannerConfig> Planners => Set<PlannerConfig>();
  public DbSet<PlannerServiceInfo> PlannerInfos => Set<PlannerServiceInfo>();
  public DbSet<PlannerSettings> PlannerSettings => Set<PlannerSettings>();
  public DbSet<AresCampaignTag> CampaignTags => Set<AresCampaignTag>();
  public DbSet<Parameter> Parameters => Set<Parameter>();
  public DbSet<DeviceLoggingSettings> DeviceLoggingSettings => Set<DeviceLoggingSettings>();
  public DbSet<DeviceState> DeviceStates => Set<DeviceState>();
  public DbSet<SilaDeviceConfig> SilaConfigs => Set<SilaDeviceConfig>();
  public DbSet<DeviceVisualizationConfig> DeviceVisualizationConfigs => Set<DeviceVisualizationConfig>();
  public DbSet<PlannerTransaction> PlannerTransactions => Set<PlannerTransaction>();
  public DbSet<AnalyzerTransaction> AnalyzerTransactions => Set<AnalyzerTransaction>();
  public DbSet<DeviceErrorHandlingConfig> DeviceErrorHandlingConfigs => Set<DeviceErrorHandlingConfig>();
  public DbSet<AresGeneralSettingsConfig> GeneralSettingsConfigs => Set<AresGeneralSettingsConfig>();
  public DbSet<CustomCommand> CustomCommands => Set<CustomCommand>();
  public DbSet<CustomCommandVersion> CustomCommandVersions => Set<CustomCommandVersion>();
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    var assembly = Assembly.GetAssembly(typeof(CoreDatabaseContext));
    if(assembly is null)
      return;

    modelBuilder.ApplyConfigurationsFromAssembly(assembly);
    base.OnModelCreating(modelBuilder);
  }

  protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
  {
    // Protobuf Data Types
    configurationBuilder.Properties<AresStruct>().HaveConversion<AresValueConverters>();
    configurationBuilder.Properties<AresValue>().HaveConversion<AresValueConverter>();

    // Protobuf Schemas
    configurationBuilder.Properties<AresValueSchema>().HaveConversion<AresValueSchemaConverter>();
    configurationBuilder.Properties<AresStructSchema>().HaveConversion<AresStructSchemaConverter>();

    // Utilities
    configurationBuilder.Properties<Timestamp>().HaveConversion<AresTimestampConverter>();

    // Ignores
    configurationBuilder.IgnoreAny<Objective>();

    base.ConfigureConventions(configurationBuilder);
  }
}
