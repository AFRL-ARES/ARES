using Ares.Datamodel.Connection;

namespace Ares.Core.Visualization;

internal class RemoteVisualizerMonitor : IDisposable
{
  private readonly RemoteVisualizer _visualizer;
  private readonly IVisualizerCache _visualizerCache;
  private readonly CancellationTokenSource _tokenSource = new();
  private readonly Task _monitorTask;
  private State _lastState = State.UnspecifiedState;

  public RemoteVisualizerMonitor(RemoteVisualizer visualizer, IVisualizerCache visualizerCache)
  {
    _visualizer = visualizer;
    _visualizerCache = visualizerCache;
    _monitorTask = Monitor(_tokenSource.Token);
  }

  public string VisualizerId => _visualizer.UniqueId;

  public void Dispose()
  {
    _tokenSource.Cancel();
    _monitorTask.ContinueWith(_ => _tokenSource.Dispose());
  }

  private Task Monitor(CancellationToken token)
  {
    return Task.Factory.StartNew(
      async () =>
      {
        while(!token.IsCancellationRequested)
        {
          await _visualizer.UpdateState();

          if(_lastState != State.Active && _visualizer.VisualizerState == State.Active)
          {
            await _visualizer.UpdateInfo();
            await _visualizer.UpdateVisualizationOptions();
            await _visualizerCache.CacheVisualizerInfo(_visualizer);
            await _visualizerCache.CacheVisualizerSettings(_visualizer);
          }

          _lastState = _visualizer.VisualizerState;
          await Task.Delay(TimeSpan.FromSeconds(5), token);
        }
      },
      token,
      TaskCreationOptions.LongRunning,
      TaskScheduler.Default).Unwrap();
  }
}