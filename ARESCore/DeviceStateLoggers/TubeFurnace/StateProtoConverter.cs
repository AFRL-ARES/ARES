using Ares.Messages.DeviceStates.TubeFurnace;
using Google.Protobuf.WellKnownTypes;
using System;
using TubeFurnace.Messaging;

namespace ARESCore.DeviceStateLoggers.TubeFurnace;

public static class StateProtoConverter
{
  public static TubeFurnaceStateEntity ToStateMessage(this TubeFurnaceState state)
  {
    var message = new TubeFurnaceStateEntity
    {
      DeviceId = state.Name,
      Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
      UniqueId = $"{Guid.NewGuid()}"
    };
    return message;
  }
}
