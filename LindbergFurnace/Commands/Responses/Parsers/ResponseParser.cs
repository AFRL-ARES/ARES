using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ares.Device.Serial;
using Ares.Device.Serial.Commands;

namespace LindbergFurnace.Commands.Responses.Parsers;

internal abstract class ResponseParser<TResponse> : SerialResponseParser<TResponse> where TResponse : CommandResponse
{
  private bool TryParseResponse(byte[][] packets, out TResponse? response, out int parsedPacketIndex)
  {
    for (var i = 0; i < packets.Length; i++)
      if (TryParseResponse(packets[i], out response))
      {
        parsedPacketIndex = i;
        return true;
      }

    parsedPacketIndex = -1;
    response = null;
    return false;
  }
  public override bool TryParseResponse(byte[] buffer, out TResponse? response, out ArraySegment<byte>? dataToRemove)
  {
    var asciiBufferProxy = Encoding.ASCII.GetString(buffer);
    var useLast = asciiBufferProxy.EndsWith($"{(char)SpecialAsciiCharacter.CR}{(char)SpecialAsciiCharacter.LF}");
    var asciiPackets = asciiBufferProxy.Split(new[] { ':', (char)SpecialAsciiCharacter.CR, (char)SpecialAsciiCharacter.LF }, StringSplitOptions.RemoveEmptyEntries).SkipLast(useLast ?0:1).ToArray();
    var availablePackets = asciiPackets.Select(Encoding.ASCII.GetBytes).ToArray();

    if (!TryParseResponse(availablePackets, out response, out var parsedLineIndex))
    {
      response = null;
      dataToRemove = null;
      return false;
    }

    var skippedBytes = availablePackets[..parsedLineIndex].Sum(s => s.Length + 2);

    var processedSize = availablePackets[parsedLineIndex].Length + 2;

    dataToRemove = new ArraySegment<byte>(buffer, skippedBytes, processedSize);
    return true;
  }

  protected abstract bool TryParseResponse(byte[] data, out TResponse? response);
}

