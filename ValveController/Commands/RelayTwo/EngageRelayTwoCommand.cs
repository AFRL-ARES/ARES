using Ares.Device.Serial.Commands;

namespace ValveController.Commands.RelayTwo;
public class EngageRelayTwoCommand : SerialCommand
{
  protected override byte[] Serialize()
  {
    return new byte[] { 3 };
  }
}
