using GenericSerialDevice.Commands.Responses;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestSerialDevice;
using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace AresService.DeviceStateLoggers.RestSerialDevice;

public class RestSerialDeviceStateLogger : IRestSerialDeviceStateLogger
{
  private readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  private readonly ISerialRestDevice _device;
  private IDisposable _stateWatcher = Disposable.Empty;

  public RestSerialDeviceStateLogger(IDbContextFactory<AresDbContext> dbContextFactory, ISerialRestDevice device)
  {
    _dbContextFactory = dbContextFactory;
    _device = device;
  }

  public string DeviceId => _device.Name;

  public void Dispose()
  {
    _stateWatcher?.Dispose();
  }

  public async Task Start()
  {
    using var context = _dbContextFactory.CreateDbContext();
    var existingInfo = await context.DeviceConfigs.FirstOrDefaultAsync(config => config.DeviceName == _device.Name && config.DeviceType == _device.GetType().FullName);
    _stateWatcher = _device.StateStream
      .Where(state => state is not null)
      .Subscribe(async state => await UpdateState(state!));
  }

  public Task Stop()
  {
    _stateWatcher.Dispose();
    return Task.CompletedTask;
  }

  private async Task UpdateState(ReadDataResponse stateResponse)
  {
    var context = _dbContextFactory.CreateDbContext();
    context.RestSerialDeviceStates.Add(stateResponse.ToStateMessage(_device));
    try
    {
      await context.SaveChangesAsync();
    }
    catch(SqlException e)
    {
      Debug.WriteLine($"Exception while saving Rest Device State: {e})");
    }
  }
}
