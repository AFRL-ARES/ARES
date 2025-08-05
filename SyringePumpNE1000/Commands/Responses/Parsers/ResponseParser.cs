using Ares.Device.Serial;
using Ares.Device.Serial.Commands;
using Ares.SyringePump.Ne1000.Messaging;
using System.Text;

namespace SyringePumpNE1000.Commands.Responses.Parsers;
public abstract class ResponseParser<T> : SerialResponseParser<T> where T : Response
{
  public ResponseParser(int address)
  {
    Address = address;
  }

  private bool TryParseResponse(string[] bufferLines, out T? response, out int parsedLineIndex)
  {
    for(var i = 0; i < bufferLines.Length; i++)
    {
      var fullStringResponse = bufferLines[i];
      var ignorableResponseData = fullStringResponse.Substring(0, fullStringResponse.IndexOf((char)SpecialAsciiCharacter.STX));
      var considerableStringResponse = fullStringResponse.Substring(ignorableResponseData.Length + 1);
      if(considerableStringResponse.Length < "##S".Length)
      {
        continue;
      }
      if(!int.TryParse(considerableStringResponse[..2], out var address))
      {
        continue;
      }

      if(address != Address)
      {
        continue;
      }

      var statusPromptStr = $"Prompt{considerableStringResponse[2]}";
      if(!Enum.TryParse<StatusPrompt>(statusPromptStr, true, out var status))
      {
        continue;
      }

      //if(considerableStringResponse.Length < 4)
      //{
      //  response = (T)Activator.CreateInstance(typeof(T), address, status)!;
      //  parsedLineIndex = i;
      //  return true;
      //}
      //if(content[0] == '?')
      //{
      //  var errorContent = content[1..];
      //  if(!errorContent.Any())
      //  {
      //    response = (T)Activator.CreateInstance(typeof(T), address, status, CommandError.UnrecognizedCommand)!;
      //    parsedLineIndex = i;
      //    return true;
      //  }
      //  if(!Enum.TryParse<CommandError>(errorContent, true, out var error))
      //  {
      //    response = (T)new Response(address, status, CommandError.UndefinedError);
      //    parsedLineIndex = i;
      //    return true;
      //  }
      //  response = (T)Activator.CreateInstance(typeof(T), address, status, error)!;
      //  parsedLineIndex = i;
      //  return true;
      //}

      var content = considerableStringResponse[3..];

      if(TryParseResponse(address, status, content, out response))
      {
        parsedLineIndex = i;
        return true;
      }
    }

    parsedLineIndex = -1;
    response = null;
    return false;
  }

  protected abstract bool TryParseResponse(int address, StatusPrompt status, string content, out T? response);

  public override bool TryParseResponse(byte[] buffer, out T? response, out ArraySegment<byte>? dataToRemove)
  {
    var asciiBufferProxy = Encoding.ASCII.GetString(buffer);
    var useLast = asciiBufferProxy.EndsWith((char)SpecialAsciiCharacter.ETX);
    var availablePackets =
      asciiBufferProxy
        .Split(new[] { (char)SpecialAsciiCharacter.ETX }, StringSplitOptions.RemoveEmptyEntries)
        .SkipLast(useLast ? 0 : 1)
        .ToArray();


    if(!TryParseResponse(availablePackets, out response, out var parsedLineIndex))
    {
      response = null;
      dataToRemove = null;
      return false;
    }

    var skippedBytes = availablePackets[..parsedLineIndex].Sum(s => s.Length + 1); // add 1 for the ETX in the buffer
    var processedSize = availablePackets[parsedLineIndex].Length + 1;// // add 1 for the ETX in the buffer
    dataToRemove = new ArraySegment<byte>(buffer, skippedBytes, processedSize);
    var test = Encoding.ASCII.GetString(dataToRemove.Value.ToArray());
    return true;
  }

  public int Address { get; }
}