namespace Ares.Core.Execution.ControlTokens;

public readonly struct ExecutionControlToken
{
  private readonly ExecutionControlTokenSource _tokenSource;

  public ExecutionControlToken(ExecutionControlTokenSource tokenSource)
  {
    _tokenSource = tokenSource;
  }

  public bool IsPaused => _tokenSource.PauseToken.IsPaused;

  public bool IsCancelled => _tokenSource.CancellationToken.IsCancellationRequested;

  /// <summary>
  /// This is here so that the cancellation token can be grabbed separately so it can be passed into things
  /// like Task.Run
  /// </summary>
  public CancellationToken CancellationToken => _tokenSource.CancellationToken;

  public void WaitForResume(CancellationToken ct)
  {
    _tokenSource.WaitForResume(ct);
  }
}
