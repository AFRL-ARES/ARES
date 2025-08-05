using Ares.Messaging.Analyzing;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Analyzing;
internal class RemoteAnalyzerMonitor : IDisposable
{
  private readonly RemoteAnalyzer _analyzer;
  private readonly Task _monitorTask;
  private readonly CancellationTokenSource _tokenSource;
  private AnalyzerState _lastState = AnalyzerState.UnspecifiedState;
  readonly IAnalyzerCache _analyzerCache;

  public RemoteAnalyzerMonitor(RemoteAnalyzer analyzer, IAnalyzerCache analyzerCache)
  {
    _analyzerCache = analyzerCache;
    _analyzer = analyzer;
    _tokenSource = new CancellationTokenSource();
    _monitorTask = Monitor(_tokenSource.Token);
  }

  public string AnalyzerId => _analyzer.UniqueId;

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
            await _analyzer.UpdateState();

            if(_lastState != AnalyzerState.Active && _analyzer.AnalyzerState == AnalyzerState.Active)
            {
              await _analyzer.UpdateInfo();
              await _analyzer.UpdateParameters();
              await _analyzer.UpdateCapabilities();
              await _analyzerCache.CacheAnalyzerInfo(_analyzer);
              await _analyzerCache.CacheAnalyzerSettings(_analyzer);
            }

            _lastState = _analyzer.AnalyzerState;

            await Task.Delay(TimeSpan.FromSeconds(5));
          }
        },
        token,
        TaskCreationOptions.LongRunning);
  }
}
