using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using LindbergFurnace;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TubeFurnace.Messaging;

namespace AresService.DeviceStateLoggers.TubeFurnace;

public class TubeFurnaceStateLogger : ITubeFurnaceStateLogger
{
  private readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  private readonly ITubeFurnace _tubeFurnace;
  private IDisposable _stateWatcher = Disposable.Empty;

  public TubeFurnaceStateLogger(IDbContextFactory<AresDbContext> dbContextFactory, ITubeFurnace tubeFurnace)
  {
    _dbContextFactory = dbContextFactory;
    _tubeFurnace = tubeFurnace;
  }

  public string DeviceId => _tubeFurnace.Name;

  public async Task Start()
  {
    using var context = _dbContextFactory.CreateDbContext();
    var existingInfo = await context.DeviceConfigs.FirstOrDefaultAsync(config => config.DeviceName == _tubeFurnace.Name && config.DeviceType == _tubeFurnace.GetType().FullName);
    _stateWatcher = _tubeFurnace.StateStream
      .Subscribe(async state => await UpdateState(state));
  }

  private async Task UpdateState(TubeFurnaceState stateResponse)
  {
    var context = _dbContextFactory.CreateDbContext();
    var state = stateResponse.ToStateMessage();
    context.TubeFurnaceStates.Add(state);
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
}