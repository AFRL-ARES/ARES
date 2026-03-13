using Ares.Toolkit.Serial.Commands;
using TicStepperController.Enums;
using TicStepperController.Responses;
using TicStepperController.Responses.Parsers;

namespace TicStepperController.Commands;

public abstract class QuickCommand(byte command) : SerialCommand
{
  protected override byte[] Serialize() => [command];
}

public class ResetCommand() : QuickCommand(0xB0);
public class EnergizeCommand() : QuickCommand(0x85);
public class DeEnergizeCommand() : QuickCommand(0x86);
public class EnterSafeStartCommand() : QuickCommand(0x8F);
public class ExitSafeStartCommand() : QuickCommand(0x83);
public class HaltAndHoldCommand() : QuickCommand(0x89);
public class ResetCommandTimeoutCommand() : QuickCommand(0x8C);

public abstract class Int32WriteCommand(byte command, int value) : SerialCommand
{
  protected override byte[] Serialize() => [command, .. value.ToByteArray()];
}

public class HaltAndSetPositionCommand(int value) : Int32WriteCommand(0xEC, value);
public class SetTargetPositionCommand(int value) : Int32WriteCommand(0xE0, value);
public class SetMaxAccelerationCommand(uint value) : Int32WriteCommand(0xEA, (int)value);
public class SetMaxDecelerationCommand(uint value) : Int32WriteCommand(0xE9, (int)value);
public class SetMaxSpeedCommand(uint value) : Int32WriteCommand(0xE6, (int)value);
public class SetStartingSpeedCommand(uint value) : Int32WriteCommand(0xE5, (int)value);

public abstract class Int7WriteCommand(byte command, byte value) : SerialCommand
{
  protected override byte[] Serialize() => [command, value];
}

public class SetCurrentLimitCommand(uint value) : Int7WriteCommand(0x91, (byte)value);
public class SetStepModeCommand(StepMode value) : Int7WriteCommand(0x94, (byte)value);

public abstract class VariableRequest<TResponse>(byte offset, byte length, SerialResponseParser<TResponse> parser) 
  : SerialCommandWithResponse<TResponse>(parser) where TResponse : SerialResponse
{
  protected override byte[] Serialize() => [0xA1, offset, length];
}

public class GetOperationStateRequest() : VariableRequest<OperationStateResponse>(0x00, 1, new OperationStateParser());
public class GetMiscFlagsRequest() : VariableRequest<MiscFlags>(0x01, 1, new MiscFlagsParser());
public class GetErrorStatusRequest() : VariableRequest<ErrorStatus>(0x02, 2, new ErrorStatusParser());
public class GetErrorsOccurredRequest() : VariableRequest<ErrorsOccurred>(0x04, 4, new ErrorsOccurredParser());
public class GetCurrentPositionRequest() : VariableRequest<CurrentPositionResponse>(0x22, 4, new CurrentPositionParser());
public class GetTargetPositionRequest() : VariableRequest<TargetPositionResponse>(0x0A, 4, new TargetPositionParser());
public class GetMaxAccelerationRequest() : VariableRequest<Uint32Response>(0x1E, 4, new Uint32Parser());
public class GetMaxDecelerationRequest() : VariableRequest<Uint32Response>(0x1A, 4, new Uint32Parser());
public class GetMaxSpeedRequest() : VariableRequest<Uint32Response>(0x16, 4, new Uint32Parser());
public class GetStartingSpeedRequest() : VariableRequest<Uint32Response>(0x12, 4, new Uint32Parser());
public class GetCurrentLimitRequest() : VariableRequest<Uint32Response>(0x4A, 4, new Uint32Parser());
public class GetStepModeRequest() : VariableRequest<StepModeResponse>(0x49, 1, new StepModeParser());
