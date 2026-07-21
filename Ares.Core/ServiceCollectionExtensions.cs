using Ares.Core.Analyzing;
using Ares.Core.AresEnvironment;
using Ares.Core.Campaigns;
using Ares.Core.CustomCommands;
using Ares.Core.DataManagement.DataMappers;
using Ares.Core.Device.Managers;
using Ares.Core.Device.Plugins.Drivers;
using Ares.Core.Device.Plugins.Drivers.Loading;
using Ares.Core.Device.Providers;
using Ares.Core.Device.Remote;
using Ares.Core.Device.Repos;
using Ares.Core.Device.State.Export;
using Ares.Core.Device.State.Export.ExportStreamProviders;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Core.Device.State.Logging;
using Ares.Core.Execution;
using Ares.Core.Execution.Executors;
using Ares.Core.Execution.Executors.Composers;
using Ares.Core.Execution.Safety;
using Ares.Core.Execution.StartConditions;
using Ares.Core.Execution.StopConditions;
using Ares.Core.Notifications;
using Ares.Core.Planning;
using Ares.Core.Resources;
using Ares.Core.Scripting;
using Ares.Core.Validation.Campaign;
using Ares.Core.Visualization.Managers;
using Ares.Core.Visualization.Providers;
using Ares.Core.Visualization.Repos;
using Ares.Datamodel.Templates;
using Microsoft.Extensions.DependencyInjection;
using Ares.Core.Settings;
using Ares.Core.Execution.StopConditions.PlannerLead;


namespace Ares.Core;

public static class ServiceCollectionExtensions
{
  /// <summary>
  /// Binds all the necessary ARES core components into an <see cref="IServiceCollection" />
  /// </summary>
  /// <param name="services"></param>
  public static void AddAresCoreComponents(this IServiceCollection services)
  {
    services.AddSingleton<IExecutionManager, ExecutionManager>();
    services.AddSingleton<IExecutionSafetyManager, ExecutionSafetyManager>();
    services.AddSingleton<IPlanningHelper, PlanningHelper>();
    services.AddSingleton<IExecutionReporter, ExecutionReporter>();
    services.AddSingleton<IExecutionReportStore, ExecutionReportStore>();
    services.AddTransient<INumExperimentsRunFactory, NumExperimentsRunFactory>();
    services.AddSingleton<IActiveCampaignTemplateStore, ActiveCampaignTemplateStore>();
    services.AddSingleton<ICampaignValidatorRepository, CampaignValidatorRepository>();
    services.AddTransient<ICampaignValidator, AllPlannersAssignedCampaignValidator>();
    services.AddTransient<ICampaignValidator, GoodAnalyzerCampaignValidator>();
    services.AddTransient<ICampaignValidator, RequiredDeviceInterpretersValidator>();
    services.AddSingleton<IDeviceDriverLoader, DeviceDriverLoader>();
    services.AddSingleton<IDeviceManager, DeviceManager>();
    services.AddSingleton<IRemoteAnalyzerManager, RemoteAnalyzerManager>();
    services.AddSingleton<IRemotePlannerManager, RemotePlannerManager>();
    services.AddSingleton<IVisualizationConfigManager, VisualizationConfigManager>();
    services.AddSingleton<IAnalyzerCache, AnalyzerCache>();
    services.AddSingleton<IRemoteDeviceManager, RemoteDeviceManager>();
    services.AddSingleton<IDeviceCache, DeviceCache>();
    services.AddSingleton<IPlannerServiceCache, PlannerServiceCache>();
    services.AddSingleton<AresVariableManager>();
    services.AddSingleton<AnalysisHelper>();
    services.AddSingleton<IDesiredAnalysisResultFactory, DesiredAnalysisResultFactory>();
    services.AddSingleton<IPlannerLeadStopConditionFactory, PlannerLeadStopConditionFactory>();
    services.AddSingleton<INotifier, Notifier>();
    services.AddSingleton<IDeviceConfigManager, DeviceConfigManager>();
    services.AddSingleton<IDriverDatabaseManager, DriverDatabaseManager>();
    services.AddSingleton<ISystemSettingsManager, SystemSettingsManager>();
    services.AddSingleton<IResourceConnectionArbiter, ResourceConnectionArbiter>();
    services.AddSingleton<ICustomCommandPersistenceService, CustomCommandPersistenceService>();
    services.AddSingleton<ICampaignTemplatePersistenceService, CampaignTemplatePersistenceService>();
    services.AddSingleton<ICampaignTemplateTransferService, CampaignTemplateTransferService>();
    services.AddSingleton<ICommandDisplayNameResolver, CommandDisplayNameResolver>();
    services.AddSingleton<CustomCommandExecutor>();

    services.AddSingleton<ISymbolProvider, DeviceSymbolProvider>();
    services.AddSingleton<ISymbolProvider, QuantitySymbolProvider>();
    services.AddSingleton<BaseEnvironmentBuilder>();

    services.AddSingleton<DeviceStateDatasetGenerator>();
    services.AddSingleton<CampaignDatasetGenerator>();

    services.BindRepositories();
    services.BindComposers();
    services.BindStartConditions();
    services.BindStateLogging();
    services.BindProviders();
  }

  private static void BindStateLogging(this IServiceCollection services)
  {
    services.AddSingleton<StateLoggerManager>();
    services.AddSingleton<IDeviceStateStreamProvider, DeviceStateStreamProvider>();
    services.AddSingleton<IDeviceStateExportStreamProvider, CombinedDeviceStateExportStreamProvider>();
    services.AddSingleton<IDeviceStateExportStreamProvider, ZippedStatesExportStreamProvider>();
    services.AddSingleton<IDeviceStateLoggerRepository, DeviceStateLoggerRepository>();
    services.AddSingleton<IDeviceStateGetter, DeviceStateGetter>();
    services.AddSingleton<IDeviceStateLoggerFactory, AresDeviceStateLoggerFactory>();
    services.AddSingleton<IAnalyzerTransactionProvider, AnalyzerTransactionProvider>();
    services.AddSingleton<IPlannerTransactionProvider, PlannerTransactionProvider>();
  }

  private static void BindStartConditions(this IServiceCollection services)
  {
    services.AddTransient<IStartCondition, CampaignInProgressStartCondition>();
    services.AddTransient<IStartCondition, AllPlannersAssignedStartCondition>();
    services.AddTransient<IStartCondition, ValidPlannerParamTypeStartCondition>();
    services.AddTransient<IStartCondition, GoodAnalyzerForExperimentOutputCondition>();
    services.AddTransient<IStartCondition, RequiredDeviceInterpretersStartCondition>();
    services.AddTransient<IStartCondition, AssignedPlannersActiveStartCondition>();
  }

  private static void BindComposers(this IServiceCollection services)
  {
    services.AddSingleton<ICommandComposer<StepTemplate, StepExecutor>, StepComposer>();
    services.AddSingleton<ICommandComposer<ExperimentTemplate, ExperimentExecutor>, ExperimentComposer>();
    services.AddSingleton<ICommandComposer<CampaignTemplate, ICampaignExecutor>, CampaignComposer>();
  }

  private static void BindProviders(this IServiceCollection services)
  {
    services.AddTransient<IAresDeviceProvider, AresDeviceProvider>();
    services.AddTransient<IDeviceDriverProvider, DeviceDriverProvider>();
    services.AddTransient<IDeviceConfigProvider, DeviceConfigProvider>();
    services.AddTransient<IDeviceVisualizationConfigProvider, DeviceVisualizationConfigProvider>();
  }

  private static void BindRepositories(this IServiceCollection services)
  {
    services.AddSingleton<IAresDeviceRepo, AresDeviceRepo>();
    services.AddSingleton<IDeviceDriverRepo, DeviceDriverRepo>();
    services.AddSingleton<IDeviceConfigRepo, DeviceConfigRepo>();
    services.AddSingleton<PlannerServiceRepo>();
    services.AddSingleton<AnalysisRepo>();
    services.AddSingleton<PlanningResponseRepo>();
    services.AddSingleton<IAnalyzerRepo, AnalyzerRepo>();
    services.AddSingleton<IPlannerServiceRepo, PlannerServiceRepo>();
    services.AddSingleton<IDeviceVisualizationConfigRepo, DeviceVisualizationConfigRepo>();
  }

}
