using Ares.Device.Serial.Commands;

namespace ValveController.Commands;
public class EnterCommandModeCommand : SerialCommand
{
  protected override byte[] Serialize()
  {
    return new byte[] { 254 };
  }
}
