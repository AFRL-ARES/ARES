using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Ares.Core.EntityConfigurations;
using Ares.Datamodel.Device;
using Ares.Messages.DeviceStates.TicStepperController;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TicStepperController;
using TicStepperController.Messaging;

namespace AresService.DeviceStateLoggers.TicStepperController;
internal class StepperControllerStateLogger : IStepperControllerStateLogger
{
  private readonly IDbContextFactory<AresDbContext> _dbContextFactory;
  private readonly IStepperController _device;
  private IDisposable _stateWatcher = Disposable.Empty;

  public StepperControllerStateLogger(IDbContextFactory<AresDbContext> dbContextFactory, IStepperController device)
  {
    _dbContextFactory = dbContextFactory;
    _device = device;
  }

  public string DeviceId => _device.UniqueId;

  public void Dispose()
  {
    _stateWatcher.Dispose();
  }

  public Task Start(DeviceLoggingSettings? settings)
  {
    _stateWatcher = _device.StateStream
      .Where(state => state.Valid)
      .Subscribe(async state => await UpdateState(state));

    return Task.CompletedTask;
  }

  public Task Stop()
  {
    _stateWatcher.Dispose();
    return Task.CompletedTask;
  }

  private async Task UpdateState(TicState state)
  {
    var statusMessages = new List<string>();

    AddIfTrue(statusMessages, state.ErrorStatus.CommandTimeout, nameof(state.ErrorStatus.CommandTimeout));
    AddIfTrue(statusMessages, state.ErrorsOccurred.EncoderSkip, nameof(state.ErrorsOccurred.EncoderSkip));
    AddIfTrue(statusMessages, state.MiscFlags.Energized, nameof(state.MiscFlags.Energized));
    AddIfTrue(statusMessages, state.ErrorStatus.ErrLineHigh, nameof(state.ErrorStatus.ErrLineHigh));
    AddIfTrue(statusMessages, state.MiscFlags.ForwardLimitActive, nameof(state.MiscFlags.ForwardLimitActive));
    AddIfTrue(statusMessages, state.MiscFlags.HomingActive, nameof(state.MiscFlags.HomingActive));
    AddIfTrue(statusMessages, state.ErrorStatus.IntentionallyDeEnergized, nameof(state.ErrorStatus.IntentionallyDeEnergized));
    AddIfTrue(statusMessages, state.ErrorStatus.KillSwitchActive, nameof(state.ErrorStatus.KillSwitchActive));
    AddIfTrue(statusMessages, state.ErrorStatus.LowVin, nameof(state.ErrorStatus.LowVin));
    AddIfTrue(statusMessages, state.ErrorStatus.MotorDriverError, nameof(state.ErrorStatus.MotorDriverError));
    AddIfTrue(statusMessages, state.MiscFlags.PositionUncertain, nameof(state.MiscFlags.PositionUncertain));
    AddIfTrue(statusMessages, state.ErrorStatus.RequiredInputInvalid, nameof(state.ErrorStatus.RequiredInputInvalid));
    AddIfTrue(statusMessages, state.MiscFlags.ReverseLimitActive, nameof(state.MiscFlags.ReverseLimitActive));
    AddIfTrue(statusMessages, state.ErrorStatus.SafeStartViolation, nameof(state.ErrorStatus.SafeStartViolation));
    AddIfTrue(statusMessages, state.ErrorsOccurred.SerialCrc, nameof(state.ErrorsOccurred.SerialCrc));
    AddIfTrue(statusMessages, state.ErrorStatus.SerialError, nameof(state.ErrorStatus.SerialError));
    AddIfTrue(statusMessages, state.ErrorsOccurred.SerialFormat, nameof(state.ErrorsOccurred.SerialFormat));
    AddIfTrue(statusMessages, state.ErrorsOccurred.SerialFraming, nameof(state.ErrorsOccurred.SerialFraming));
    AddIfTrue(statusMessages, state.ErrorsOccurred.SerialRxOverrun, nameof(state.ErrorsOccurred.SerialRxOverrun));

    var saveState = new TicStepperControllerState()
    {
      StepMode = (Ares.Messages.DeviceStates.TicStepperController.StepMode)state.StepMode,
      CurrentPosition = state.CurrentPosition,
      CustomStepSize = state.CustomStepSize,
      DeviceId = DeviceId,
      MaxAcceleration = state.MaxAcceleration,
      MaxDeceleration = state.MaxDeceleration,
      MaxSpeed = state.MaxSpeed,
      StartingSpeed = state.StartingSpeed,
      TargetPosition = state.TargetPosition,
      Timestamp = DateTime.UtcNow.ToTimestampUtc(),
      UniqueId = Guid.NewGuid().ToString()
    };

    saveState.StatusMessages.AddRange(statusMessages);
    await using var dbContext = _dbContextFactory.CreateDbContext();
    dbContext.TicStepperControllerStates.Add(saveState);

    // sometimes the context times out for some reason and we don't want
    // to just crash the service. Although this only happened during debugging
    // so far, so this may not be a problem during normal use.
    try
    {
      await dbContext.SaveChangesAsync();
    }
    catch(SqlException e)
    {
      Debug.WriteLine($"Exception while saving MFC State: {e})");
    }
  }

  private static void AddIfTrue(IList<string> list, bool condition, string name)
  {
    if(!condition)
      return;

    list.Add(name);
  }

  public async Task UpdateSettings(DeviceLoggingSettings settings)
  {
    await Stop();
    await Start(settings);
  }
}
