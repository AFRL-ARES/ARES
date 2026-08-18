using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ares.Services;
using Ares.Core.Grpc.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Health.V1;
using UI.Application.Hosting;
using UI.Infrastructure.Grpc;

namespace UI.Features.ServerHealth;

/// <summary>
/// Responsible for using an ares server info client to get state messages from the server and publishing them
/// to an internal observable
/// </summary>
internal class ServerHealthService : ILocalService
{
  private readonly AresServerInfoService _aresServerInfo;
  private readonly HealthCheckService _healthClient;
  private readonly ILogger<ServerHealthService> _logger;
  private readonly ISubject<ServerStatusResponse> _serverStatusSubject = new BehaviorSubject<ServerStatusResponse>(new ServerStatusResponse { ServerStatus = Ares.Services.ServerStatus.Idle, StatusMessage = "Not Connected" });

  public readonly IObservable<ServerStatusResponse> ServerStatus;
  private Task _heartbeatListener = Task.CompletedTask;
  private CancellationTokenSource _serviceCancellationTokenSource = new();
  private Task _stateListener = Task.CompletedTask;

  public ServerHealthService(AresServerInfoService aresServerInfo, HealthCheckService healthClient, ILogger<ServerHealthService> logger)
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
      var info = await _aresServerInfo.GetServerInfo(new Empty(), null);
      ServerName = info.ServerName;
      ServerVersion = Version.Parse(info.Version);
      AresConnectionStatus = AresConnectionStatus.Connected;
    }
    catch (Exception e)
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
        var healthTask = _healthClient.Check(healthRequest, null);
        var completedTask = await Task.WhenAny(timeoutTask, healthTask);

        if (completedTask == timeoutTask)
        {
          HandleDisconnect("Lost connection to server. Health check timed out.");
          return;
        }

        if (completedTask.IsFaulted)
        {
          HandleDisconnect(completedTask.Exception?.Message ?? "Health check faulted");
          return;
        }

        await Task.Delay(timeout);
      }
      catch (Exception e)
      {
        HandleDisconnect(e.Message);
      }
  }

  private void HandleDisconnect(string message = "")
  {
    ServerName = string.Empty;
    ServerVersion = new Version();
    AresConnectionStatus = AresConnectionStatus.Disconnected;
    _serverStatusSubject.OnNext(new ServerStatusResponse
    {
      ServerStatus = Ares.Services.ServerStatus.Error,
      StatusMessage = $"Server is offline: {message}"
    });

    _logger.LogError("Disconnected from server: {}", message);

    Stop();
  }

  private async Task StartListening()
  {
    var stream = new LocalStream<ServerStatusResponse>();
    _ = _aresServerInfo.GetServerStatusStream(new Empty(), stream, null);
    try
    {
      while (await stream.MoveNext(_serviceCancellationTokenSource.Token) && !_serviceCancellationTokenSource.IsCancellationRequested)
      {
        var response = stream.Current;
        _logger.LogInformation("Received state from server: {}", response.StatusMessage);
        _serverStatusSubject.OnNext(response);
      }
    }
    catch (Exception e)
    {
      _logger.LogError("Failed to get state from server: {}", e.Message);
      HandleDisconnect(e.Message);
    }
  }
}
