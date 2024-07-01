using TicStepperController.Commands.Enums;

namespace TicStepperController.Proto.Extensions;
public static class StepModeExtensions
{
  public static StepMode ToInternal(this Messaging.StepMode stepMode) => stepMode switch
  {
    Messaging.StepMode.Undefined => StepMode.Undefined,
    Messaging.StepMode.Step12 => StepMode.Step1_2,
    Messaging.StepMode.Step14 => StepMode.Step1_4,
    Messaging.StepMode.Step18 => StepMode.Step1_8,
    Messaging.StepMode.Step116 => StepMode.Step1_16,
    Messaging.StepMode.Step132 => StepMode.Step1_32,
    Messaging.StepMode.Step12100 => StepMode.Step1_2_100,
    Messaging.StepMode.Step164 => StepMode.Step1_64,
    Messaging.StepMode.Step1128 => StepMode.Step1_128,
    Messaging.StepMode.Step1256 => StepMode.Step1_256,
    _ => throw new ArgumentOutOfRangeException($"{stepMode}"),
  };

  public static Messaging.StepMode ToProto(this StepMode stepMode) => stepMode switch
  {
    StepMode.Undefined => Messaging.StepMode.Undefined,
    StepMode.Step1_2 => Messaging.StepMode.Step12,
    StepMode.Step1_4 => Messaging.StepMode.Step14,
    StepMode.Step1_8 => Messaging.StepMode.Step18,
    StepMode.Step1_16 => Messaging.StepMode.Step116,
    StepMode.Step1_32 => Messaging.StepMode.Step132,
    StepMode.Step1_2_100 => Messaging.StepMode.Step12100,
    StepMode.Step1_64 => Messaging.StepMode.Step164,
    StepMode.Step1_128 => Messaging.StepMode.Step1128,
    StepMode.Step1_256 => Messaging.StepMode.Step1256,
    _ => throw new ArgumentOutOfRangeException($"{stepMode}"),
  };
}
