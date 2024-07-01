using Ares.Device.Serial.Commands;

namespace TicStepperController.Commands;
public class Int32WriteCommand : SerialCommand
{
  public Int32WriteCommand(byte command, int value)
  {
    Command = command;
    Value = value;
  }
  public int Value { get; }
  public byte Command { get; }
  protected override byte[] Serialize()
  {
    var valueBytes = Value.ToByteArray();
    var arr = new byte[] { Command };
    arr = arr.Concat(valueBytes).ToArray();
    return arr;
  }
}
