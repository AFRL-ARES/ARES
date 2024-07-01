using AlicatMFC;
using Ares.Core.EntityConfigurations;
using ARESCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace ARESCore.DeviceStateLoggers.Mfc;

public class MfcStateLogger : IMfcStateLogger
{
  private readonly IDbContextFactory<ARESDbContext> _dbContextFactory;
  private readonly IMassFlowController _device;
  private IDisposable _stateWatcher = Disposable.Empty;

  public MfcStateLogger(IDbContextFactory<ARESDbContext> dbContextFactory, IMassFlowController device)
  {
    _dbContextFactory = dbContextFactory;
    _device = device;
  }

  public string DeviceId => _device.Name;

  public void Dispose()
  {
    _stateWatcher.Dispose();
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

  public async Task UpdateState(DateTime timestamp)
  {
    var state = await _device.StateStream.Take(1);
    if (state.LiveData is null)
      return;

    await using var context = _dbContextFactory.CreateDbContext();
    var mfcState = new Ares.Messages.DeviceStates.Mfc.MfcState
    {
      Timestamp = timestamp.ToTimestampUtc(),
      UniqueId = Guid.NewGuid().ToString(),
      AbsolutePressure = state.LiveData.AbsolutePressure?.PoundsForcePerSquareInch,
      Gas = state.LiveData.Gas,
      MassFlow = state.LiveData.MassFlow?.StandardCubicCentimetersPerMinute,
      VolumetricFlow = state.LiveData.VolumetricFlow?.CubicCentimetersPerMinute,
      Setpoint = state.LiveData.Setpoint?.StandardCubicCentimetersPerMinute,
      DeviceId = state.Name,
      Temperature = state.LiveData.Temperature?.DegreesCelsius
    };

    mfcState.StatusCodes.AddRange(state.LiveData.StatusCodes.Select(s => s.ToString()));

    context.MfcStates.Add(mfcState);
    await context.SaveChangesAsync();
  }

  private async Task UpdateState(MfcState state)
  {
    if (state.LiveData is null)
      return;

    await using var context = _dbContextFactory.CreateDbContext();
    var time = DateTime.UtcNow;
    var mfcState = new Ares.Messages.DeviceStates.Mfc.MfcState
    {
      Timestamp = time.ToTimestampUtc(),
      UniqueId = Guid.NewGuid().ToString(),
      AbsolutePressure = state.LiveData.AbsolutePressure?.PoundsForcePerSquareInch,
      Gas = state.LiveData.Gas,
      MassFlow = state.LiveData.MassFlow?.StandardCubicCentimetersPerMinute,
      VolumetricFlow = state.LiveData.VolumetricFlow?.CubicCentimetersPerMinute,
      Setpoint = state.LiveData.Setpoint?.StandardCubicCentimetersPerMinute,
      DeviceId = state.Name,
      Temperature = state.LiveData.Temperature?.DegreesCelsius
    };

    mfcState.StatusCodes.AddRange(state.LiveData.StatusCodes.Select(s => s.ToString()));
    context.MfcStates.Add(mfcState);
    // sometimes the context times out for some reason and we don't want
    // to just crash the service. Although this only happened during debugging
    // so far, so this may not be a problem during normal use.
    try
    {
      await context.SaveChangesAsync();
    }
    catch (SqlException e)
    {
      Debug.WriteLine($"Exception while saving MFC State: {e})");
    }
  }
}
