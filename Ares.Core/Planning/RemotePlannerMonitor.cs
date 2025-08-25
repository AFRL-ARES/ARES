using Ares.Datamodel.Connection;

namespace Ares.Core.Planning;

public class RemotePlannerMonitor : IDisposable
{
  private readonly RemotePlannerService _planner;
  private readonly Task _monitorTask;
  private readonly CancellationTokenSource _tokenSource;
  private State _lastState = State.UnspecifiedState;
  readonly IPlannerServiceCache _plannerCache;

  public RemotePlannerMonitor(RemotePlannerService planner, IPlannerServiceCache plannerCache)
  {
    _plannerCache = plannerCache;
    _planner = planner;
    _tokenSource = new CancellationTokenSource();
    _monitorTask = Monitor(_tokenSource.Token);
  }

  public string PlannerId => _planner.UniqueId;

  public void Dispose()
  {
    _tokenSource.Cancel();
    _monitorTask.ContinueWith(_ => _tokenSource.Dispose());
  }

  private Task Monitor(CancellationToken token)
  {
    return Task.Factory
      .StartNew(
      async (_) =>
      {
        while(!token.IsCancellationRequested)
        {
          await _planner.UpdateState();

          if(_lastState != State.Active && _planner.PlannerServiceState == State.Active)
          {
            await _planner.UpdateInfo();
            await _planner.UpdateCapabilities();
            await _plannerCache.CachePlannerInfo(_planner);
            await _plannerCache.CachePlannerSettings(_planner);
          }

          _lastState = _planner.PlannerServiceState;
          await Task.Delay(TimeSpan.FromSeconds(5));
        }
      },
      token,
      TaskCreationOptions.LongRunning);
  }
}
