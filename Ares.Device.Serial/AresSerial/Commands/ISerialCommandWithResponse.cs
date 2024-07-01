using System;

namespace Ares.Device.Serial.Commands;

internal interface ISerialCommandWithResponse
{
  Guid Id { get; internal set; }
  ISerialResponseParser ResponseParser { get; }
}
