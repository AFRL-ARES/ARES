using Ares.Device.Serial.Commands;

namespace TicStepperController.Commands;
public class QuickCommand : SerialCommand
{
  public QuickCommand(byte command)
  {
    Command = command;
  }

  public byte Command { get; set; }

  protected override byte[] Serialize()
  {
    return new byte[] { Command };
  }
}
