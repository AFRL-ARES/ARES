using Ares.Toolkit.Serial.Commands;

namespace ValveController.Commands;
public class EnableAllDevicesCommand : SerialCommand
{
  protected override byte[] Serialize()
  {
    return new byte[] { 248 };
  }
}
