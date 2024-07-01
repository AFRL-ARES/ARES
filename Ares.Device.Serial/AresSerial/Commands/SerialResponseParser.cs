using System;
using System.Collections.Generic;
using System.Linq;

namespace Ares.Device.Serial.Commands;

public abstract class SerialResponseParser<T> : ISerialResponseParser where T : SerialResponse
{
  public abstract bool TryParseResponse(byte[] buffer, out T? response, out ArraySegment<byte>? dataToRemove);

  bool ISerialResponseParser.TryParseResponse(SerialBlock[] buffer, out SerialResponse? response, out ArraySegment<byte>? dataToRemove)
  {
    var bytes = buffer.SelectMany(b => b.Data).ToArray();
    var responseParsed = TryParseResponse(bytes, out T? typedResponse, out var toRemove);
    response = typedResponse;
    dataToRemove = toRemove;
    return responseParsed;
  }
}
