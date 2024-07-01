using System;
using Google.Protobuf;

namespace Ares.Device.Serial.Commands;

public abstract class SerialResponse
{
  public Guid RequestId { get; internal set; }
}
