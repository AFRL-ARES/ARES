using Ares.Device.Serial.Commands;

namespace TicStepperController.Commands.Responses.Parsers;
public abstract class VariableParser<T> : SerialResponseParser<T> where T : SerialResponse
{
  public VariableParser(int expectedBits)
  {
    ExpectedBits = expectedBits;
  }

  public override bool TryParseResponse(byte[] buffer, out T? response, out ArraySegment<byte>? dataToRemove)
  {
    if(buffer.Length < ExpectedBits)
    {
      response = default;
      dataToRemove = default;
      return false;
    }

    try
    {
      response = ParseResponse(buffer[0..ExpectedBits]);
      dataToRemove = new ArraySegment<byte>(buffer, 0, ExpectedBits);
      return true;
    }
    catch(Exception)
    {
      response = default;
      dataToRemove = default;
      return false;
    }
  }

  protected abstract T ParseResponse(byte[] buffer);

  public int ExpectedBits { get; }
}
