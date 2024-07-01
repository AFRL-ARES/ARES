using System;

namespace Ares.Device.Serial.Commands;

public abstract class SerialCommand
{
  public Guid Id { get; internal set; } = Guid.NewGuid();

  internal byte[] SerializedData => Serialize();

  protected abstract byte[] Serialize();
}
