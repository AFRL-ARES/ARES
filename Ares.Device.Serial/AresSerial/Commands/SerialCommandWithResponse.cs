using System;

namespace Ares.Device.Serial.Commands;

public abstract class SerialCommandWithResponse<T> : SerialCommand, ISerialCommandWithResponse where T : SerialResponse
{
  public SerialCommandWithResponse(SerialResponseParser<T> parser)
  {
    Parser = parser;
  }

  internal SerialResponseParser<T> Parser { get; }

  Guid ISerialCommandWithResponse.Id { get; set; }
  ISerialResponseParser ISerialCommandWithResponse.ResponseParser => Parser;
}
