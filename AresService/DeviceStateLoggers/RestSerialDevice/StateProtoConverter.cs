using System;
using Ares.Messages.DeviceStates.RestSerialDevice;
using GenericSerialDevice.Commands.Responses;
using Google.Protobuf.WellKnownTypes;
using RestSerialDevice;

namespace AresService.DeviceStateLoggers.RestSerialDevice;

public static class StateProtoConverter
{
  public static RestSerialDeviceStateEntity ToStateMessage(this ReadDataResponse response, ISerialRestDevice device)
  {
    var message = new RestSerialDeviceStateEntity
    {
      DeviceId = device.DeviceId,
      Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
      UniqueId = $"{Guid.NewGuid()}"
    };

    message.Values.Add(response.Values);
    return message;
  }
}
