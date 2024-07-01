using System.IO.Ports;

public class SerialTester
{
  private static readonly SerialPort port = new("COM7", 9600, Parity.None, 8, StopBits.One);
  private static readonly SerialPort port2 = new("COM5", 9600, Parity.None, 8, StopBits.One);

  [STAThread]
  private static void Main(string[] args)
  {
    Console.WriteLine("Incoming Data:");
    DumpPositionString();
    // Attach a method to be called when there
    // is data waiting in the port's buffer 
    //port.DataReceived += port_DataReceived;
    //port2.DataReceived += Port2_DataReceived;
    // Begin communications
    port.Open();
    //port2.Open();
    // Enter an application loop to keep this thread alive 
    Console.WriteLine("ready");
    byte[] commandMode = { 254 };
    var enable = 248;
    var disable = 249;
    byte[] getBothStatus = { 7 };
    byte[] turnOn = { 3 };

    while (true)
    {
      var input = Console.ReadLine();
      port.Write(commandMode, 0, 1);
      port.Write(turnOn, 0, 1);

      input = Console.ReadLine();
      port.Write(commandMode, 0, 1);
      port.Write(getBothStatus, 0, 1);
      var boop = port.ReadByte();
      Console.WriteLine(boop);

      input = Console.ReadLine();
      port.Write(commandMode, 0, 1);
      port.Write(turnOn, 0, 1);
      boop = port.ReadByte();
      Console.WriteLine(boop);

    }
  }

  private static void DumpPositionString()
  {
    ushort pos = 480; // target position
    byte playtime = 0x32; // how long to take. Each value is 11.2 ms (so 0x32 is 560 ms)
    byte id = 1; // ID of the servo
    var upper = (byte)(pos >> 8);
    var lower = (byte)(pos & 0xff);
    var dat = new byte[] { lower, upper, 0, id, playtime };
    GenCommand(05, id, dat);
  }

  private static void Port2_DataReceived(object sender, SerialDataReceivedEventArgs e)
  {
    // Show all the incoming data in the port's buffer
    var bytes = new List<byte>();
    for (var i = 0; i < port2.BytesToRead; i++)
    {
      var byter = (byte)port2.ReadByte();
      bytes.Add(byter);
    }

    if (bytes.Count == 0)
      return;
    var hexString = BitConverter.ToString(bytes.ToArray());
    //if (!hexString.Equals("FF-FF-07-01-07-00-FE"))
    Console.WriteLine(hexString);
  }

  private static void GenCommand(byte cmd, byte pid, byte[] data)
  {
    byte[] header = { 0xff, 0xff };
    var size = (byte)(7 + data.Length);
    var xorDat = new byte();

    foreach (var t in data)
      xorDat = (byte)(xorDat ^ t);

    var checksum1 = (byte)((size ^ pid ^ cmd ^ xorDat) & 0xfe);
    var checksum2 = (byte)(~checksum1 & 0xFE);
    var bytes = new List<byte>();
    bytes.AddRange(header);
    bytes.Add(size);
    bytes.Add(pid);
    bytes.Add(cmd);
    bytes.Add(checksum1);
    bytes.Add(checksum2);
    bytes.AddRange(data);
    var hexString = BitConverter.ToString(bytes.ToArray());
    Console.WriteLine(hexString);
  }

  private static void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
  {
    // Show all the incoming data in the port's buffer
    var bytes = new List<byte>();
    for (var i = 0; i < port.BytesToRead; i++)
    {
      var byter = (byte)port.ReadByte();

      bytes.Add(byter);
    }

    if (bytes.Count == 0)
      return;
    var hexString = BitConverter.ToString(bytes.ToArray());
    //Console.WriteLine(hexString);
  }
}