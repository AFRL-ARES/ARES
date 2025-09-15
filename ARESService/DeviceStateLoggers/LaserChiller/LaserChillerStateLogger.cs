using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Ares.Core.EntityConfigurations;
using Ares.Datamodel.Device;
using Ares.Messages.DeviceStates.Chiller;
using LaserChiller;
using LaserChiller.Commands.Responses;
using Microsoft.EntityFrameworkCore;

namespace AresService.DeviceStateLoggers.LaserChiller;

public class LaserChillerStateLogger : ILaserChillerStateLogger
{
  private readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  private readonly ILaserChiller _device;
  private IDisposable _stateWatcher = Disposable.Empty;

  public LaserChillerStateLogger(IDbContextFactory<AresDbContext> dbContextFactory, ILaserChiller device)
  {
    _dbContextFactory = dbContextFactory;
    _device = device;
  }

  public string DeviceId => _device.UniqueId;

  public DeviceLoggingSettings Settings { get; private set; } = new DeviceLoggingSettings { LoggingType = DeviceLoggingSettings.Types.LoggingType.None };

  public Task Stop()
  {
    _stateWatcher.Dispose();
    return Task.CompletedTask;
  }

  public void Dispose()
  {
    _stateWatcher?.Dispose();
  }

  public async Task Start(DeviceLoggingSettings? settings)
  {
    using var context = _dbContextFactory.CreateDbContext();
    var existingInfo = await context.DeviceConfigs.FirstOrDefaultAsync(config => config.UniqueId == _device.UniqueId && config.DeviceType == _device.GetType().FullName);
    _stateWatcher = _device.StateStream
      .Where(state => state is not null)
      .Subscribe(async state => await UpdateState(state!));
  }

  public async Task UpdateState(DateTime timestamp)
  {
    var state = await _device.StateStream.Take(1);
    await using var context = _dbContextFactory.CreateDbContext();
    var chillerState = new ChillerState
    {
      Timestamp = timestamp.ToTimestampUtc(),
      UniqueId = Guid.NewGuid().ToString(),
      ManifoldTemperature = state.Temperature,
      DeviceId = _device.UniqueId
    };

    context.ChillerStates.Add(chillerState);
    await context.SaveChangesAsync();
  }

  private async Task UpdateState(GetManifoldTemperatureResponse state)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var time = DateTime.UtcNow;

    var chillerState = new ChillerState
    {
      Timestamp = time.ToTimestampUtc(),
      UniqueId = Guid.NewGuid().ToString(),
      ManifoldTemperature = state?.Temperature,
      DeviceId = _device.UniqueId
    };

    context.ChillerStates.Add(chillerState);
    await context.SaveChangesAsync();
  }

  public async Task UpdateSettings(DeviceLoggingSettings settings)
  {
    await Stop();
    await Start(settings);
  }
}
