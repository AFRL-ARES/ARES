using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Ares.Core.EntityConfigurations;
using Ares.Messages.DeviceStates.Tc0304;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TC0304;
using TC0304.Commands;

namespace AresService.DeviceStateLoggers.Tc0304;
public class Tc0304StateLogger : ITc0304StateLogger
{
  private readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  private readonly IDataloggerThermometer _device;
  private IDisposable _stateWatcher = Disposable.Empty;

  public Tc0304StateLogger(IDbContextFactory<AresDbContext> dbContextFactory, IDataloggerThermometer device)
  {
    _dbContextFactory = dbContextFactory;
    _device = device;
  }

  public string DeviceId => _device.UniqueId;

  public Task Stop()
  {
    _stateWatcher.Dispose();
    return Task.CompletedTask;
  }

  public void Dispose()
  {
    _stateWatcher.Dispose();
  }

  public async Task Start()
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
    var tc0304State = new Tc0304State
    {
      Timestamp = timestamp.ToTimestampUtc(),
      UniqueId = Guid.NewGuid().ToString(),
      Probe1Temperature = state.T1Probe?.DegreesCelsius,
      Probe2Temperature = state.T2Probe?.DegreesCelsius,
      Probe3Temperature = state.T3Probe?.DegreesCelsius,
      Probe4Temperature = state.T4Probe?.DegreesCelsius,
      DeviceId = _device.UniqueId
    };

    context.Tc0304States.Add(tc0304State);
    // sometimes the context times out for some reason and we don't want
    // to just crash the service. Although this only happened during debugging
    // so far, so this may not be a problem during normal use.
    try
    {
      await context.SaveChangesAsync();
    }
    catch(SqlException e)
    {
      Debug.WriteLine($"Exception while saving MFC State: {e})");
    }
  }

  private async Task UpdateState(DataResponse state)
  {
    await using var context = _dbContextFactory.CreateDbContext();
    var time = DateTime.UtcNow;
    var tc0304State = new Tc0304State
    {
      Timestamp = time.ToTimestampUtc(),
      UniqueId = Guid.NewGuid().ToString(),
      Probe1Temperature = state.T1Probe?.DegreesCelsius,
      Probe2Temperature = state.T2Probe?.DegreesCelsius,
      Probe3Temperature = state.T3Probe?.DegreesCelsius,
      Probe4Temperature = state.T4Probe?.DegreesCelsius,
      DeviceId = _device.UniqueId
    };

    context.Tc0304States.Add(tc0304State);
    await context.SaveChangesAsync();
  }
}
