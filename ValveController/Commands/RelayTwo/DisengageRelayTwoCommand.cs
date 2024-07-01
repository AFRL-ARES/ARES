using Ares.Device.Serial.Commands;

namespace ValveController.Commands.RelayTwo;
public class DisengageRelayTwoCommand : SerialCommand
{
  protected override byte[] Serialize()
  {
    return new byte[] { 2 };
  }
}
