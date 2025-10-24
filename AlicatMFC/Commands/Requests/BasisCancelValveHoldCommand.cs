namespace AlicatMFC.Commands.Requests;

internal class BasisCancelValveHoldCommand : MfcCommand
{
  public BasisCancelValveHoldCommand(char id, string firmware) : base(id, firmware)
  {
  }

  protected override string SerializeToString()
    => $"C";
}
