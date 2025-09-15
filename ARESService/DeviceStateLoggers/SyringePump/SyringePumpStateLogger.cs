using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Ares.Datamodel.Device;
using Ares.SyringePump.Ne1000.Messaging;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SyringePumpNE1000;

namespace AresService.DeviceStateLoggers.SyringePump;
public class SyringePumpStateLogger : ISyringePumpStateLogger
{
  private readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  private readonly ISyringePump _syringePump;
  private IDisposable _stateWatcher = Disposable.Empty;

  public SyringePumpStateLogger(IDbContextFactory<AresDbContext> dbContextFactory, ISyringePump syringePump)
  {
    _dbContextFactory = dbContextFactory;
    _syringePump = syringePump;
  }

  public string DeviceId => _syringePump.UniqueId;

  public async Task Start(DeviceLoggingSettings? settings)
  {
    using var context = _dbContextFactory.CreateDbContext();
    var existingInfo = await context.DeviceConfigs.FirstOrDefaultAsync(config => config.UniqueId == _syringePump.UniqueId && config.DeviceType == _syringePump.GetType().FullName);
    _stateWatcher = _syringePump.StateStream
      .Subscribe(async state => await UpdateState(state));
  }

  private async Task UpdateState(StateResponse stateResponse)
  {
    var context = _dbContextFactory.CreateDbContext();
    var state = stateResponse.ToStateMessage();
    context.SyringePumpStates.Add(state);
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

  public void Dispose()
  {
    _stateWatcher.Dispose();
  }

  public Task Stop()
  {
    _stateWatcher.Dispose();
    return Task.CompletedTask;
  }

  public async Task UpdateSettings(DeviceLoggingSettings settings)
  {
    await Stop();
    await Start(settings);
  }
}
