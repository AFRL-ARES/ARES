namespace Ares.Core.Execution.ControlTokens;

public class PauseToken
{
  private readonly PauseTokenSource _source;

  internal PauseToken(PauseTokenSource source)
  {
    _source = source;
  }

  public bool IsPaused => _source.IsPaused;

  public void Wait(CancellationToken token)
  {
    _source.Wait(token);
  }
}
