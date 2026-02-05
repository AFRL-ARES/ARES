namespace AresScript;

public readonly struct ScriptExecutionControlToken
{
  private readonly CancellationToken _cancellationToken;
  private readonly ScriptExecutionControlTokenSource? _source;

  public ScriptExecutionControlToken(CancellationToken cancellationToken)
  {
    _cancellationToken = cancellationToken;
    _source = null;
  }

  internal ScriptExecutionControlToken(CancellationToken cancellationToken, ScriptExecutionControlTokenSource source)
  {
    _cancellationToken = cancellationToken;
    _source = source;
  }

  public bool IsCancellationRequested => _cancellationToken.IsCancellationRequested;

  public bool IsPaused => _source?.IsPaused ?? false;

  public CancellationToken CancellationToken => _cancellationToken;

  public void ThrowIfCancellationRequested()
  {
    _cancellationToken.ThrowIfCancellationRequested();
  }

  public ValueTask WaitForResumeAsync()
  {
    return _source is null
      ? ValueTask.CompletedTask
      : _source.WaitForResumeAsync(_cancellationToken);
  }
}

public sealed class ScriptExecutionControlTokenSource : IDisposable
{
  private readonly CancellationTokenSource _cancellationTokenSource;
  private readonly CancellationTokenSource? _linkedCancellationTokenSource;
  private readonly Lock _pauseLock = new();
  private TaskCompletionSource<bool>? _pauseSignal;
  private bool _disposed;

  public ScriptExecutionControlTokenSource(CancellationToken cancellationToken = default)
  {
    _cancellationTokenSource = new CancellationTokenSource();
    if(cancellationToken.CanBeCanceled)
    {
      _linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
        _cancellationTokenSource.Token,
        cancellationToken);
    }
  }

  public ScriptExecutionControlToken Token
  {
    get
    {
      ThrowIfDisposed();
      var token = _linkedCancellationTokenSource?.Token ?? _cancellationTokenSource.Token;
      return new ScriptExecutionControlToken(token, this);
    }
  }

  public bool IsPaused
  {
    get
    {
      lock(_pauseLock)
      {
        ThrowIfDisposed();
        return _pauseSignal is not null;
      }
    }
  }

  public bool IsCancellationRequested => (_linkedCancellationTokenSource?.IsCancellationRequested ?? false)
    || _cancellationTokenSource.IsCancellationRequested;

  public void Pause()
  {
    lock(_pauseLock)
    {
      ThrowIfDisposed();
      _pauseSignal ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }
  }

  public void Resume()
  {
    TaskCompletionSource<bool>? pauseSignal;
    lock(_pauseLock)
    {
      ThrowIfDisposed();
      pauseSignal = _pauseSignal;
      _pauseSignal = null;
    }

    pauseSignal?.TrySetResult(true);
  }

  public void Cancel()
  {
    ThrowIfDisposed();
    _cancellationTokenSource.Cancel();
  }

  internal ValueTask WaitForResumeAsync(CancellationToken cancellationToken)
  {
    Task? pauseTask;
    lock(_pauseLock)
    {
      ThrowIfDisposed();
      pauseTask = _pauseSignal?.Task;
    }

    if(pauseTask is null)
    {
      return ValueTask.CompletedTask;
    }

    return cancellationToken.CanBeCanceled
      ? new ValueTask(pauseTask.WaitAsync(cancellationToken))
      : new ValueTask(pauseTask);
  }

  public void Dispose()
  {
    TaskCompletionSource<bool>? pauseSignal;
    lock(_pauseLock)
    {
      if(_disposed)
      {
        return;
      }

      _disposed = true;
      pauseSignal = _pauseSignal;
      _pauseSignal = null;
    }

    pauseSignal?.TrySetCanceled();
    _linkedCancellationTokenSource?.Dispose();
    _cancellationTokenSource.Dispose();
  }

  private void ThrowIfDisposed()
  {
    ObjectDisposedException.ThrowIf(_disposed, this);
  }
}
