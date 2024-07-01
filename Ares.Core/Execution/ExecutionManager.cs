using Ares.Core.Execution.ControlTokens;
using Ares.Core.Execution.Executors;
using Ares.Core.Execution.Executors.Composers;
using Ares.Core.Execution.StartConditions;
using Ares.Core.Execution.StopConditions;
using Ares.Messaging;
using DynamicData;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Execution;

public class ExecutionManager : IExecutionManager
{
  private readonly IActiveCampaignTemplateStore _activeCampaignTemplateStore;
  private readonly ICommandComposer<CampaignTemplate, ICampaignExecutor> _campaignComposer;
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContext;
  private readonly IEnumerable<IStartCondition> _startConditions;
  private ExecutionControlTokenSource? _executionControlTokenSource;

  public ExecutionManager(IEnumerable<IStartCondition> startConditions,
    IDbContextFactory<CoreDatabaseContext> dbContext,
    IActiveCampaignTemplateStore activeCampaignTemplateStore,
    ICommandComposer<CampaignTemplate, ICampaignExecutor> campaignComposer)
  {
    _startConditions = startConditions;
    _dbContext = dbContext;
    _activeCampaignTemplateStore = activeCampaignTemplateStore;
    _campaignComposer = campaignComposer;
  }

  public IList<IStopCondition> CampaignStopConditions { get; } = new List<IStopCondition>();

  public bool CanRun => _startConditions.All(condition => condition.CanStart()?.Success ?? true) && _activeCampaignTemplateStore.CampaignTemplate is not null;

  public async Task Start()
  {
    CheckCampaignStartPrerequisites();
    var executor = _campaignComposer.Compose(_activeCampaignTemplateStore.CampaignTemplate!);
    executor.StopConditions.Add(CampaignStopConditions);
    _executionControlTokenSource = new ExecutionControlTokenSource();
    var campaignResult = await executor.Execute(_executionControlTokenSource.Token);
    await PostExecution(campaignResult);
  }

  public void Stop()
    => _executionControlTokenSource?.Cancel();

  public void Pause()
    => _executionControlTokenSource?.Pause();

  public void Resume()
    => _executionControlTokenSource?.Resume();

  private void CheckCampaignStartPrerequisites()
  {
    if (_activeCampaignTemplateStore.CampaignTemplate is null)
      throw new InvalidOperationException("CampaignTemplate was not assigned to the active template store.");

    var startConditionResults = _startConditions.Select(condition => condition.CanStart()).Where(result => result is not null && !result.Success).ToArray();
    if (startConditionResults.Any())
      throw new InvalidOperationException($"Failed to start campaign:{Environment.NewLine}{string.Join(Environment.NewLine, startConditionResults.SelectMany(conditionResult => conditionResult!.Messages))}");
  }

  private async Task PostExecution(CampaignResult result)
  {
    //await StoreCompletedCampaign(result);
    _executionControlTokenSource?.Dispose();
    _executionControlTokenSource = null;
  }

  private async Task StoreCompletedCampaign(CampaignResult result)
  {
    await using var context = await _dbContext.CreateDbContextAsync();
    context.CampaignResults.Add(result);
    await context.SaveChangesAsync();
  }
}
