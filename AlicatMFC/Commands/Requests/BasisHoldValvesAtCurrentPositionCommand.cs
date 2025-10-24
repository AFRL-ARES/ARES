namespace AlicatMFC.Commands.Requests;

internal class BasisHoldValvesAtCurrentPositionCommand : MfcCommand
{
  private readonly double _currentValveDrive;

  public BasisHoldValvesAtCurrentPositionCommand(char id, string firmware, double currentValveDrive) : base(id, firmware)
  {
    _currentValveDrive = currentValveDrive;
  }

  protected override string SerializeToString()
    => $"HPUR {_currentValveDrive}";
}
