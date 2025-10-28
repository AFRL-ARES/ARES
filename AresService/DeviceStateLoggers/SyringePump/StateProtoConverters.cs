using System;
using Ares.Core.EntityConfigurations;
using Ares.Messages.DeviceStates.SyringePump;

namespace AresService.DeviceStateLoggers.SyringePump;

internal static class StateProtoConverters
{
  public static RateUnit ToStateMessage(this Ares.SyringePump.Ne1000.Messaging.RateUnit unit) => unit switch
  {
    Ares.SyringePump.Ne1000.Messaging.RateUnit.UndefinedRateUnit => RateUnit.UndefinedRateUnit,
    Ares.SyringePump.Ne1000.Messaging.RateUnit.Um => RateUnit.MicrolitersPerMinute,
    Ares.SyringePump.Ne1000.Messaging.RateUnit.Mm => RateUnit.MillilitersPerMinute,
    Ares.SyringePump.Ne1000.Messaging.RateUnit.Uh => RateUnit.MicrolitersPerHour,
    Ares.SyringePump.Ne1000.Messaging.RateUnit.Mh => RateUnit.MillilitersPerHour,
    _ => RateUnit.UndefinedRateUnit
  };

  public static VolumeUnit ToStateMessage(this Ares.SyringePump.Ne1000.Messaging.VolumeUnit unit) => unit switch
  {
    Ares.SyringePump.Ne1000.Messaging.VolumeUnit.UndefinedVolumeUnit => VolumeUnit.UndefinedVolumeUnit,
    Ares.SyringePump.Ne1000.Messaging.VolumeUnit.Ul => VolumeUnit.Microliters,
    Ares.SyringePump.Ne1000.Messaging.VolumeUnit.Ml => VolumeUnit.Milliliters,
    _ => VolumeUnit.UndefinedVolumeUnit
  };

  public static Status ToStateMessage(this Ares.SyringePump.Ne1000.Messaging.StatusPrompt status) => status switch
  {
    Ares.SyringePump.Ne1000.Messaging.StatusPrompt.UndefinedStatusPrompt => Status.Undefined,
    Ares.SyringePump.Ne1000.Messaging.StatusPrompt.PromptI => Status.Infusing,
    Ares.SyringePump.Ne1000.Messaging.StatusPrompt.PromptW => Status.Withdrawing,
    Ares.SyringePump.Ne1000.Messaging.StatusPrompt.PromptS => Status.PumpingStopped,
    Ares.SyringePump.Ne1000.Messaging.StatusPrompt.PromptP => Status.PumpingPaused,
    Ares.SyringePump.Ne1000.Messaging.StatusPrompt.PromptT => Status.TimedPausePhase,
    Ares.SyringePump.Ne1000.Messaging.StatusPrompt.PromptU => Status.OperationalTriggerWait,
    Ares.SyringePump.Ne1000.Messaging.StatusPrompt.PromptX => Status.Purging,
    _ => Status.Undefined
  };

  public static SyringePumpState ToStateMessage(this Ares.SyringePump.Ne1000.Messaging.StateResponse state)
  {
    var pumpState = new SyringePumpState
    {
      UniqueId = Guid.NewGuid().ToString(),
      Timestamp = DateTime.UtcNow.ToTimestampUtc(),
      DispensedVolume = state.DispensedVolume,
      WithdrawnVolume = state.WithdrawnVolume,
      VolumeUnit = state.VolumeUnits.ToStateMessage(),
      RateUnit = state.RateUnits.ToStateMessage(),
      Address = state.Address,
      Status = state.Status.ToStateMessage(),
      DeviceId = state.DeviceId
    };
    return pumpState;
  }
}
