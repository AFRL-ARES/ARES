using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;

namespace UI.Backend.Devices;

public class RemoteDeviceAdapterMonitor(IAresDeviceAdapter deviceAdapter, ILogger<RemoteDeviceAdapterMonitor> logger) : IDisposable
{
    private IDisposable _deviceStatusWatcher = Disposable.Empty;
    private Task _reconnectLoop = Task.CompletedTask;
    private CancellationTokenSource _reconnectCts = new();

    // Interlocked-backed state
    private int _reconnectInProgress = 0; // 0=false, 1=true
    private int _reconnectAttempts = 0;
    private int _activated = 0;           // 0=false, 1=true

    public void Activate()
    {
        // ensure one-time activation
        if (Interlocked.Exchange(ref _activated, 1) == 1)
        {
            logger.LogDebug("Monitor already activated {}.", deviceAdapter.Name);
            return;
        }

        _deviceStatusWatcher = deviceAdapter.ConnectionStatusStream
            .DistinctUntilChanged()
            .Subscribe(async status =>
            {
                try
                {
                    if (status == ConnectionStatus.Disconnected)
                    {
                        StartReconnectionLoop();
                    }
                    else if (status == ConnectionStatus.Connected)
                    {
                        await EndReconnectionLoop().ConfigureAwait(false);
                        logger.LogInformation("Device connected; activating adapter.");
                        await deviceAdapter.Activate().ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error handling status {Status}", status);
                }
            });
    }

    private void StartReconnectionLoop()
    {
        // fast exit if already running
        if (Interlocked.Exchange(ref _reconnectInProgress, 1) == 1)
            return;

        // reset/replace CTS defensively
        try { _reconnectCts.Cancel(); } catch { /* ignore */ }
        _reconnectCts.Dispose();
        _reconnectCts = new CancellationTokenSource();

        Interlocked.Exchange(ref _reconnectAttempts, 0);
        logger.LogWarning("Starting reconnection loop.");

        _reconnectLoop = Task.Run(async () =>
        {
            var token = _reconnectCts.Token;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await deviceAdapter.UpdateConnectionStatus().ConfigureAwait(false);

                    var attempts = Interlocked.Increment(ref _reconnectAttempts);

                    // linear backoff (cap 30s)
                    var baseSeconds = Math.Min(attempts * 5, 30);

                    // jitter ±20%
                    var jitter = (int)Math.Round(baseSeconds * 0.2);
                    var waitSeconds = Math.Max(1, baseSeconds + Random.Shared.Next(-jitter, jitter + 1));

                    logger.LogInformation("Reconnect attempt {Attempt}. Waiting {Delay}s.", attempts, waitSeconds);
                    await Task.Delay(TimeSpan.FromSeconds(waitSeconds), token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break; // expected on cancellation
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Reconnection attempt errored.");
                    try { await Task.Delay(TimeSpan.FromSeconds(2), token).ConfigureAwait(false); } catch { break; }
                }
            }
        }, _reconnectCts.Token);
    }

    private async Task EndReconnectionLoop()
    {
        // only if we were running
        if (Interlocked.Exchange(ref _reconnectInProgress, 0) == 0)
            return;

        logger.LogInformation("Ending reconnection loop.");

        try
        {
            await _reconnectCts.CancelAsync().ConfigureAwait(false);
            try
            {
                await _reconnectLoop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
        }
        finally
        {
            _reconnectCts.Dispose();
            _reconnectCts = new CancellationTokenSource();
            Interlocked.Exchange(ref _reconnectAttempts, 0);
        }
    }

    public void Dispose()
    {
        // best-effort, non-throwing shutdown
        try { _deviceStatusWatcher.Dispose(); } catch { /* ignore */ }

        try
        {
            // cancel loop quickly; avoid long blocking in Dispose()
            _ = EndReconnectionLoop();
        }
        catch { /* ignore */ }

        GC.SuppressFinalize(this);
    }
}