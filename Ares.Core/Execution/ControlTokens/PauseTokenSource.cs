namespace Ares.Core.Execution.ControlTokens;

internal class PauseTokenSource : IDisposable
{
  private TaskCompletionSource<bool> _tcs;
  private readonly object _syncLock = new();
  private bool _disposed;

  public PauseTokenSource()
  {
    _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    _tcs.SetResult(true);
  }

  public bool IsPaused
  {
    get
    {
      lock(_syncLock) return !_tcs.Task.IsCompleted;
    }
  }

  public PauseToken Token
  {
    get
    {
      ThrowIfDisposed();
      return new PauseToken(this);
    }
  }

  public void Dispose()
  {
    _disposed = true;
  }

  public void Pause()
  {
    ThrowIfDisposed();
    lock(_syncLock)
    {
      if(_tcs.Task.IsCompleted)
      {
        _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      }
    }
  }

  public void Resume()
  {
    ThrowIfDisposed();
    lock(_syncLock)
    {
      _tcs.TrySetResult(true);
    }
  }

  public void Wait(CancellationToken token)
  {
    ThrowIfDisposed();
    try
    {
      _tcs.Task.Wait(token);
    }
    catch(OperationCanceledException) { }
    catch(AggregateException ex) when(ex.InnerException is OperationCanceledException) { }
  }

  public async Task WaitAsync(CancellationToken token)
  {
    ThrowIfDisposed();
    try
    {
      await _tcs.Task.WaitAsync(token);
    }
    catch(OperationCanceledException)
    {
    }
  }

  private void ThrowIfDisposed()
  {
    if(_disposed)
      ThrowObjectDisposedException(GetType().Name);
  }

  private static void ThrowObjectDisposedException(string name)
  {
    throw new ObjectDisposedException(name);
  }
}