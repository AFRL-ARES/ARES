using System.IO.Ports;
using System.Text.RegularExpressions;
using Ares.Device.Serial;

namespace SyringePumpNE1000;

public class SyringePumpConnection : AresHardwareConnection, ISyringePumpConnection
{
  private static Regex temp = new(@"\x02[^\x03]*\x03");
  private static Regex _basicResponseRegex = new(@$"\x{(int)SpecialAsciiCharacter.STX:x2}[]");

  public SyringePumpConnection(string portName) : base(new SerialPortConnectionInfo(19200, Parity.None, 8, StopBits.One), portName)
  {
  }
}
