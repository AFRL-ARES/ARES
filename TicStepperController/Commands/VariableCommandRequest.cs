using Ares.Device.Serial.Commands;

namespace TicStepperController.Commands;
public class VariableCommandRequest<TResponse> : SerialCommandWithResponse<TResponse> where TResponse : SerialResponse
{
  public VariableCommandRequest(byte offset, byte length, SerialResponseParser<TResponse> parser) : base(parser)
  {
    Offset = offset;
    Length = length;
  }

  public byte Offset { get; }
  public byte Length { get; }

  protected override byte[] Serialize()
  {
    return new byte[] { 0xA1, Offset, Length };
  }
}
