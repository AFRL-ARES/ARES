using Ares.Device.Serial.Commands;

namespace ValveController.Commands.RelayOne;
public class EngageRelayOneCommand : SerialCommand
{
  protected override byte[] Serialize()
  {
    return new byte[] { 1 };
  }
}
