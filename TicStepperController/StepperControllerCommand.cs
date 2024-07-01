namespace TicStepperController;
public enum StepperControllerCommand
{
  Reset,
  EnterSafeStart,
  ExitSafeStart,
  HaltAndHold,
  HaltAndSetPosition,
  SetTargetPosition,
  NextStep,
  PreviousStep,
}
