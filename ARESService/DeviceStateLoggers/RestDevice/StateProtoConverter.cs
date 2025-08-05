using Ares.Messages.DeviceStates.RestDevice;
using GenericSerialDevice.Commands.Responses;
using Google.Protobuf.WellKnownTypes;
using RestSerialDevice;
using System;

namespace AresService.DeviceStateLoggers.RestDevice;

public static class StateProtoConverter
{
  public static RestDeviceStateEntity ToStateMessage(this ReadDataResponse response, ISerialRestDevice device)
  {
    var message = new RestDeviceStateEntity
    {
      DeviceId = device.DeviceId,
      Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
      UniqueId = $"{Guid.NewGuid()}"
    };

    message.Values.Add(response.Values);
    return message;
  }
}
