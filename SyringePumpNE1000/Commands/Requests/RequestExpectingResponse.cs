using Ares.Device.Serial;
using Ares.Device.Serial.Commands;
using NullFX.CRC;
using SyringePumpNE1000.Commands.Responses;
using System.Text;

namespace SyringePumpNE1000.Commands.Requests;

internal abstract class RequestExpectingResponse<TResponse> : SerialCommandWithResponse<TResponse> where TResponse : Response
{
  public RequestExpectingResponse(SerialResponseParser<TResponse> parser, bool safeMode = false) : base(parser)
  {
    SafeMode = safeMode;
  }


  private byte[] GenerateBasicOutboundCommand(string commandData)
  {
    var utf16CommandDataChars = $"{commandData}\r".ToCharArray();
    var asciiCommandDataEntry = Encoding.Convert(Encoding.Default,
      Encoding.ASCII,
      Encoding.Default.GetBytes(utf16CommandDataChars));

    var basicCommand = asciiCommandDataEntry.ToArray();
    return basicCommand;
  }

  private byte[] GenerateSafeOutboundCommand(string commandData)
  {
    var utf16CommandDataChars = commandData.ToCharArray();
    var asciiCommandDataEntry = Encoding.Convert(Encoding.Default,
      Encoding.ASCII,
      Encoding.Default.GetBytes(utf16CommandDataChars));

    var packetLength =
      1 + asciiCommandDataEntry.Length +
      2;// TODO: Determine if STX and ETX should be included in the length. Current form: Length[1] + CommandData[input.Length] + CRC16[2]

    var lengthEntry = (byte)packetLength;
    var crc16 = Crc16.ComputeChecksum(Crc16Algorithm.Ccitt, asciiCommandDataEntry);
    var crc16HighEntry = (byte)(crc16 >> 8);
    var crc16LowEntry = (byte)crc16;

    var safeCommandData = new List<byte>
    {
      (byte)SpecialAsciiCharacter.STX,
      lengthEntry
    };
    safeCommandData.AddRange(asciiCommandDataEntry);
    safeCommandData.Add(crc16HighEntry);
    safeCommandData.Add(crc16LowEntry);
    safeCommandData.Add((byte)SpecialAsciiCharacter.ETX);

    var safeCommand = safeCommandData.ToArray();
    return safeCommand;
  }

  protected sealed override byte[] Serialize()
  {
    var commandString = GenerateCommandString();
    if(SafeMode)
    {
      return GenerateSafeOutboundCommand(commandString);
    }

    return GenerateBasicOutboundCommand(commandString);
  }

  protected abstract string GenerateCommandString();

  public bool SafeMode { get; }
}
