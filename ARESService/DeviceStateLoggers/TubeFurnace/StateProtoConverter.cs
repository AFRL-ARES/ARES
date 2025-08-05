using Ares.Messages.DeviceStates.TubeFurnace;
using Google.Protobuf.WellKnownTypes;
using System;
using TubeFurnace.Messaging;

namespace AresService.DeviceStateLoggers.TubeFurnace;

public static class StateProtoConverter
{
  public static TubeFurnaceStateEntity ToStateMessage(this TubeFurnaceState state)
  {
    var message = new TubeFurnaceStateEntity
    {
      DeviceId = state.Name,
      Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
      UniqueId = $"{Guid.NewGuid()}",
      CurrentTemp = state.CurrentTemperature,
      SetPointTemp = state.Setpoint
    };

    return message;
  }
}
