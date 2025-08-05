using Ares.Core.Analyzing;
using Ares.Core.AresEnvironment;
using Ares.Core.Notifications;
using Ares.Core.Planning;
using Ares.Messaging;

namespace Ares.Core.Execution.Executors.Composers;

public class CampaignComposer : ICommandComposer<CampaignTemplate, ICampaignExecutor>
{
  private readonly IExecutionReporter _executionReporter;
  private readonly ICommandComposer<ExperimentTemplate, ExperimentExecutor> _experimentComposer;
  private readonly ICommandComposer<ExperimentTemplate, StartupScriptExecutor> _startupScriptComposer;
  private readonly ICommandComposer<ExperimentTemplate, CloseoutScriptExecutor> _closeoutScriptComposer;
  private readonly IPlanningHelper _planningHelper;
  private readonly IEnumerable<IExecutionSummaryHandler> _resultHandlers;
  private readonly IEnumerable<INotificationHandler> _notificationHandlers;
  private readonly AresVariableManager _variableManager;
  readonly AnalysisHelper _analysisHelper;
  readonly AnalysisRepo _analysisRepo;
  readonly IAnalyzerRepo _analyzerRepo;

  public CampaignComposer(AnalysisHelper analysisHelper,
    ICommandComposer<ExperimentTemplate, ExperimentExecutor> experimentComposer,
    ICommandComposer<ExperimentTemplate, StartupScriptExecutor> startupScriptComposer,
    ICommandComposer<ExperimentTemplate, CloseoutScriptExecutor> closeoutScriptComposer,
    IPlanningHelper planningHelper,
    IExecutionReporter executionReporter,
    IEnumerable<IExecutionSummaryHandler> resultHandlers,
    AnalysisRepo analysisRepo,
    IAnalyzerRepo analyzerRepo,
    IEnumerable<INotificationHandler> notificationHandlers,
    AresVariableManager variableManager)
  {
    _analyzerRepo = analyzerRepo;
    _analysisRepo = analysisRepo;
    _analysisHelper = analysisHelper;
    _variableManager = variableManager;
    _experimentComposer = experimentComposer;
    _startupScriptComposer = startupScriptComposer;
    _closeoutScriptComposer = closeoutScriptComposer;
    _planningHelper = planningHelper;
    _executionReporter = executionReporter;
    _resultHandlers = resultHandlers;
    _notificationHandlers = notificationHandlers;
  }

  public ICampaignExecutor Compose(CampaignTemplate template)
    => new CampaignExecutor(_experimentComposer, _startupScriptComposer, _closeoutScriptComposer, _planningHelper, _executionReporter, _analysisHelper, template, _resultHandlers, _analysisRepo, _notificationHandlers, _analyzerRepo, _variableManager);
}
