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

  public CancellationToken CancellationToken => _tokenSource.CancellationToken;

  public PauseToken PauseToken => _tokenSource.PauseToken;

  public void WaitForResume(CancellationToken ct)
  {
    _tokenSource.WaitForResume(ct);
  }

  public void Pause()
  {
    _tokenSource.Pause();
  }
}
