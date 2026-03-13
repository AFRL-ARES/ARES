using Ares.Toolkit.Serial.Commands;
using TicStepperController.Commands;
using TicStepperController.Enums;

namespace TicStepperController.Responses.Parsers;

public abstract class VariableParser<T> : SerialResponseParser<T> where T : SerialResponse
{
  protected readonly int ExpectedBytes;
  protected VariableParser(int expectedBytes) => ExpectedBytes = expectedBytes;

  public override bool TryParseResponse(byte[] buffer, out T? response, out ArraySegment<byte>? dataToRemove)
  {
    if (buffer.Length < ExpectedBytes)
    {
      response = null;
      dataToRemove = null;
      return false;
    }

    try
    {
      response = ParseResponse(buffer[..ExpectedBytes]);
      dataToRemove = new ArraySegment<byte>(buffer, 0, ExpectedBytes);
      return true;
    }
    catch
    {
      response = null;
      dataToRemove = null;
      return false;
    }
  }

  protected abstract T ParseResponse(byte[] data);
}

public class OperationStateParser() : VariableParser<OperationStateResponse>(1)
{
  protected override OperationStateResponse ParseResponse(byte[] data) => new((OperationState)data[0]);
}

public class CurrentPositionParser() : VariableParser<CurrentPositionResponse>(4)
{
  protected override CurrentPositionResponse ParseResponse(byte[] data) => new(data.ToInt32());
}

public class TargetPositionParser() : VariableParser<TargetPositionResponse>(4)
{
  protected override TargetPositionResponse ParseResponse(byte[] data) => new(data.ToInt32());
}

public class StepModeParser() : VariableParser<StepModeResponse>(1)
{
  protected override StepModeResponse ParseResponse(byte[] data) => new((StepMode)data[0]);
}

public class Uint32Parser() : VariableParser<Uint32Response>(4)
{
  protected override Uint32Response ParseResponse(byte[] data) => new((uint)data.ToInt32());
}

public class ErrorStatusParser() : VariableParser<ErrorStatus>(2)
{
  protected override ErrorStatus ParseResponse(byte[] buffer)
  {
    var firstNum = buffer[0];
    var secondNum = buffer[1];
    return new ErrorStatus(
      (firstNum & 0b0000_0001) != 0,
      (firstNum & 0b0000_0010) != 0,
      (firstNum & 0b0000_0100) != 0,
      (firstNum & 0b0000_1000) != 0,
      (firstNum & 0b0001_0000) != 0,
      (firstNum & 0b0010_0000) != 0,
      (firstNum & 0b0100_0000) != 0,
      (firstNum & 0b1000_0000) != 0,
      (secondNum & 0b0000_0001) != 0
    );
  }
}

public class ErrorsOccurredParser() : VariableParser<ErrorsOccurred>(4)
{
  protected override ErrorsOccurred ParseResponse(byte[] buffer)
  {
    var val = buffer[2];
    return new ErrorsOccurred(
      (val & 0b0001) != 0,
      (val & 0b0010) != 0,
      (val & 0b0100) != 0,
      (val & 0b1000) != 0,
      (val & 0b1_0000) != 0
    );
  }
}

public class MiscFlagsParser() : VariableParser<MiscFlags>(1)
{
  protected override MiscFlags ParseResponse(byte[] buffer)
  {
    var val = buffer[0];
    return new MiscFlags(
      (val & 0b0000_0001) != 0,
      (val & 0b0000_0010) != 0,
      (val & 0b0000_0100) != 0,
      (val & 0b0000_1000) != 0,
      (val & 0b0001_0000) != 0
    );
  }
}
