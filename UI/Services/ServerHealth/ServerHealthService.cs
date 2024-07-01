using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ares.Messaging;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Health.V1;

namespace UI.Services.ServerHealth;

/// <summary>
/// Responsible for using an ares server info client to get state messages from the server and publishing them
/// to an internal observable
/// </summary>
internal class ServerHealthService : ILocalService
{
  private readonly AresServerInfo.AresServerInfoClient _aresServerInfo;
  private readonly Health.HealthClient _healthClient;
  private readonly ILogger<ServerHealthService> _logger;
  private readonly ISubject<ServerStatusResponse> _serverStatusSubject = new BehaviorSubject<ServerStatusResponse>(new ServerStatusResponse { ServerStatus = Ares.Messaging.ServerStatus.Idle, StatusMessage = "Not Connected" });

  public readonly IObservable<ServerStatusResponse> ServerStatus;
  private Task _heartbeatListener = Task.CompletedTask;
  private CancellationTokenSource _serviceCancellationTokenSource = new();
  private Task _stateListener = Task.CompletedTask;

  public ServerHealthService(AresServerInfo.AresServerInfoClient aresServerInfo, Health.HealthClient healthClient, ILogger<ServerHealthService> logger)
  {
    _aresServerInfo = aresServerInfo;
    _healthClient = healthClient;
    _logger = logger;
    ServerStatus = _serverStatusSubject.AsObservable();
  }

  private bool Running => !_heartbeatListener.IsCompleted || !_stateListener.IsCompleted;

  public AresConnectionStatus AresConnectionStatus { get; private set; }

  public string ServerName { get; private set; } = string.Empty;

  public Version ServerVersion { get; private set; } = new();

  public async Task Start()
  {
    if (Running)
      return;

    _serviceCancellationTokenSource = new CancellationTokenSource();
    AresConnectionStatus = AresConnectionStatus.Connecting;
    try
    {
      var info = await _aresServerInfo.GetServerInfoAsync(new Empty(), null, null, _serviceCancellationTokenSource.Token);
      ServerName = info.ServerName;
      ServerVersion = Version.Parse(info.Version);
      AresConnectionStatus = AresConnectionStatus.Connected;
    }
    catch (RpcException e)
    {
      AresConnectionStatus = AresConnectionStatus.Disconnected;
      ServerName = string.Empty;
      ServerVersion = new Version();
      _logger.LogError("Failed to start {}: {}", GetType().Name, e.Message);
      return;
    }

    _stateListener = Task.Run(StartListening, _serviceCancellationTokenSource.Token);
    _heartbeatListener = Task.Run(EstablishHeartbeat, _serviceCancellationTokenSource.Token);
  }

  public async void Stop()
  {
    _serviceCancellationTokenSource.Cancel();

    await Task.WhenAll(_heartbeatListener, _stateListener);
  }

  private async void EstablishHeartbeat()
  {
    var healthRequest = new HealthCheckRequest { Service = string.Empty };
    while (!_serviceCancellationTokenSource.Token.IsCancellationRequested)
      try
      {
        var timeout = TimeSpan.FromSeconds(15);
        var timeoutTask = Task.Delay(timeout, _serviceCancellationTokenSource.Token);
        var healthTask = _healthClient.CheckAsync(healthRequest, null, null, _serviceCancellationTokenSource.Token).ResponseAsync;
        var completedTask = await Task.WhenAny(timeoutTask, healthTask);

        if (completedTask == timeoutTask)
        {
          HandleDisconnect("Lost connection to server. Health check timed out.");
          return;
        }

        if (completedTask.IsFaulted && completedTask.Exception?.InnerException is RpcException e)
        {
          HandleDisconnect(e.Message);
          return;
        }

        await Task.Delay(timeout);
      }
      catch (RpcException e)
      {
        HandleDisconnect(e.Message);
      }
      catch (OperationCanceledException)
      {
        HandleDisconnect($"{GetType().Name} stopping.");
      }
  }

  private void HandleDisconnect(string message = "")
  {
    ServerName = string.Empty;
    ServerVersion = new Version();
    AresConnectionStatus = AresConnectionStatus.Disconnected;
    _serverStatusSubject.OnNext(new ServerStatusResponse
    {
      ServerStatus = Ares.Messaging.ServerStatus.Error,
      StatusMessage = $"Server is offline: {message}"
    });

    _logger.LogError("Disconnected from server: {}", message);

    Stop();
  }

  private async Task StartListening()
  {
    var statusStream = _aresServerInfo.GetServerStatusStream(new Empty(), null, null, _serviceCancellationTokenSource.Token);
    try
    {
      while (await statusStream.ResponseStream.MoveNext(_serviceCancellationTokenSource.Token) && !_serviceCancellationTokenSource.IsCancellationRequested)
      {
        var response = statusStream.ResponseStream.Current;
        _logger.LogInformation("Received state from server: {}", response.StatusMessage);
        _serverStatusSubject.OnNext(response);
      }
    }
    catch (RpcException e)
    {
      _logger.LogError("Failed to get state from server: {}", e.Message);
      HandleDisconnect(e.Message);
    }
  }
}
