using System;

namespace Ares.Device.Serial.Commands;

internal interface ISerialResponseParser
{
  bool TryParseResponse(SerialBlock[] buffer, out SerialResponse? response, out ArraySegment<byte>? dataToRemove);
}
