// See https://aka.ms/new-console-template for more information
using MfcTerminal;
using Microsoft.Win32;
using System.Globalization;
using System.IO.Ports;
using System.Net;
using System.Text;

static int Main()
{
  var port = new SerialPort("COM9", 9600, Parity.None, 7, StopBits.One);
  port.NewLine = "\r\n";

  port.Open();


  Task.Run(() => {
    while (true)
    {
      Console.Write($"{port.ReadExisting()}");
    }
  });

  Console.WriteLine("Ready!");
  //while (true)
  {
    //var input = Console.ReadLine().Split();
    var input = new[] {"11", "3", "392", "4"};
    var testInput = ":110303920004\r\n";
    var blah = Lrc(testInput.Select(s => (byte)s).ToArray());
    var argIndex = 0;
    var address = int.Parse(input[argIndex++]);
    var functionCode = int.Parse(input[argIndex++]);
    var startRegister = int.Parse(input[argIndex++]);
    var numRegisters = int.Parse(input[argIndex++]);
    var messageData = new List<byte>();
    messageData.Add((byte)address);
    messageData.Add((byte)functionCode);
    messageData.AddRange(GenerateFunctionDataBytes(startRegister, numRegisters));
    var lrc = Lrc(messageData.ToArray());
    Console.WriteLine($"[Dec]{lrc}, {lrc:X2}, ");
    var functionData = GenerateFunctionData(startRegister, numRegisters);
    var message = Serialize(address, functionCode, functionData);
    //Console.WriteLine($"{Lrc(input.SelectMany(str => Encoding.ASCII.GetBytes(str)))}");
    Console.WriteLine($"Sending Message: {Encoding.ASCII.GetString(message)}");
    port.WriteLine($"{message}");
  }
  return 0;
}



static byte[] Serialize(int address, int functionCode, string functionData)
{
  var messageStr = $":{address:00}{functionCode:00}{functionData}";

  var serialData = Encoding.ASCII.GetBytes(messageStr);
  return serialData;
}

static byte Lrc(byte[] data)
{
  var sum = data.Aggregate((l, r) => (byte)(l ^ r));
  var complement = ~sum + 1;
  var lrc = (byte)complement;
  return lrc;
}

static string GenerateFunctionData(int startRegister, int numRegisters)
{
  return $"{startRegister:0000}{numRegisters:0000}";
}

static byte[] GenerateFunctionDataBytes(int startRegister, int numRegisters)
{
  var startRegisterInt = (int)startRegister;
  var startRegisterStr = $"{startRegisterInt:X4}";
  var registerStartUpperStr = startRegisterStr[..2];
  var registerStartLowerStr = startRegisterStr[2..];
  var registerStartUpper = byte.Parse(registerStartUpperStr, NumberStyles.HexNumber);
  var registerStartLower = byte.Parse(registerStartLowerStr, NumberStyles.HexNumber);

  var numRegistersStr = $"{numRegisters:X4}";
  var numRegistersUpperStr = numRegistersStr[..2];
  var numRegistersLowerStr = numRegistersStr[2..];
  var numRegistersUpper = byte.Parse(numRegistersUpperStr, NumberStyles.HexNumber);
  var numRegistersLower = byte.Parse(numRegistersLowerStr, NumberStyles.HexNumber);

  var functionData = new[] { registerStartUpper, registerStartLower, numRegistersUpper, numRegistersLower };
  return functionData;
}

var blah = new BlackBoxServo();

await blah.Start();

//var blah = new BlackBoxStepper();

//await blah.Start();