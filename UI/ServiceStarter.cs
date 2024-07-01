using UI.Services;
using UI.Services.ServerHealth;
using UI.Services.ServerHealthNotification;

namespace UI;

internal class ServiceStarter : ILocalService
{
  private readonly ServerHealthService _healthService;
  private readonly ServerHealthNotificationService _serverHealthNotificationService;

  public ServiceStarter(ServerHealthService healthService,
    ServerHealthNotificationService serverHealthNotificationService)
  {
    _healthService = healthService;
    _serverHealthNotificationService = serverHealthNotificationService;
  }

  public async Task Start()
  {
    await _healthService.Start();
    await _serverHealthNotificationService.Start();
  }
}