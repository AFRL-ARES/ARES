using Ares.Device.Serial.Commands;

namespace TicStepperController.Commands;
public class Int7WriteCommand : SerialCommand
{
  protected Int7WriteCommand(byte command, byte value)
  {
    Command = command;
    Value = value;
  }
  public byte Value { get; }
  public byte Command { get; set; }

  protected override byte[] Serialize()
  {
    return new byte[] { Command, Value };
  }
}
