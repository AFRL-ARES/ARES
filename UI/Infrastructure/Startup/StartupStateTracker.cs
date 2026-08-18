namespace UI.Infrastructure.Startup;

public class StartupStateTracker
{
  public bool IsReady { get; private set; } = false;

  public event Action? OnSystemReady;

  public void MarkAsReady()
  {
    IsReady = true;
    OnSystemReady?.Invoke();
  }
}
