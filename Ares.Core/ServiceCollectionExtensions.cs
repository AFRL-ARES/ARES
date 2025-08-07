using Ares.Core.Analyzing;
using Ares.Core.AresEnvironment;
using Ares.Core.Device;
using Ares.Core.Execution;
using Ares.Core.Execution.Executors;
using Ares.Core.Execution.Executors.Composers;
using Ares.Core.Execution.StartConditions;
using Ares.Core.Execution.StopConditions;
using Ares.Core.Planning;
using Ares.Core.Validation.Campaign;
using Ares.Datamodel.Templates;
using Microsoft.Extensions.DependencyInjection;

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
    services.AddSingleton<IPlanningHelper, PlanningHelper>();
    services.AddSingleton<IPlannerManager, PlannerManager>();
    services.AddSingleton<IExecutionReporter, ExecutionReporter>();
    services.AddSingleton<IExecutionReportStore, ExecutionReportStore>();
    services.AddSingleton<IAnalyzerRepo, AnalyzerRepo>();
    services.AddTransient<INumExperimentsRunFactory, NumExperimentsRunFactory>();
    services.AddSingleton<IActiveCampaignTemplateStore, ActiveCampaignTemplateStore>();
    services.AddSingleton<ICampaignValidatorRepository, CampaignValidatorRepository>();
    services.AddTransient<ICampaignValidator, AllPlannersAssignedCampaignValidator>();
    services.AddTransient<ICampaignValidator, GoodAnalyzerCampaignValidator>();
    services.AddTransient<ICampaignValidator, RequiredDeviceInterpretersValidator>();
    services.AddSingleton<IDeviceCommandInterpreterRepo, DeviceCommandInterpreterRepo>();
    services.AddSingleton<IRemoteAnalyzerManager, RemoteAnalyzerManager>();
    services.AddSingleton<IAnalyzerCache, AnalyzerCache>();
    services.AddSingleton<AresVariableManager>();
    services.AddSingleton<AnalysisRepo>();
    services.AddSingleton<AnalysisHelper>();
    services.AddSingleton<IDesiredAnalysisResultFactory, DesiredAnalysisResultFactory>();

    services.BindComposers();
    services.BindStartConditions();
  }

  private static void BindStartConditions(this IServiceCollection services)
  {
    services.AddTransient<IStartCondition, CampaignInProgressStartCondition>();
    services.AddTransient<IStartCondition, AllPlannersAssignedStartCondition>();
    services.AddTransient<IStartCondition, GoodAnalyzerForExperimentOutputCondition>();
    services.AddTransient<IStartCondition, RequiredDeviceInterpretersStartCondition>();
    services.AddTransient<IStartCondition, AssignedPlannersActiveStartCondition>();
  }

  private static void BindComposers(this IServiceCollection services)
  {
    services.AddSingleton<ICommandComposer<StepTemplate, StepExecutor>, StepComposer>();
    services.AddSingleton<ICommandComposer<ExperimentTemplate, ExperimentExecutor>, ExperimentComposer>();
    services.AddSingleton<ICommandComposer<ExperimentTemplate, StartupScriptExecutor>, StartupComposer>();
    services.AddSingleton<ICommandComposer<ExperimentTemplate, CloseoutScriptExecutor>, CloseoutComposer>();
    services.AddSingleton<ICommandComposer<CampaignTemplate, ICampaignExecutor>, CampaignComposer>();
  }
}
