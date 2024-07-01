using Ares.Device.Serial.Commands;

namespace ValveController.Commands.RelayOne;
public class DisengageRelayOneCommand : SerialCommand
{
  protected override byte[] Serialize()
  {
    return new byte[] { 0 };
  }
}
