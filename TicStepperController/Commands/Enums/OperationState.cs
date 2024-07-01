namespace TicStepperController.Commands.Enums;
public enum OperationState
{
  Reset = 0,
  DeEnergized = 2,
  SoftError = 4,
  WaitingForErrLine = 6,
  StartingUp = 8,
  Normal = 10
}
