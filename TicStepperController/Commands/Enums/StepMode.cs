namespace TicStepperController.Commands.Enums;
public enum StepMode
{
  Undefined = 0, // custom field, not provided by the device
  Step1_2 = 1,
  Step1_4 = 2,
  Step1_8 = 3,
  Step1_16 = 4,
  Step1_32 = 5,
  Step1_2_100 = 6,
  Step1_64 = 7,
  Step1_128 = 8,
  Step1_256 = 9
}
