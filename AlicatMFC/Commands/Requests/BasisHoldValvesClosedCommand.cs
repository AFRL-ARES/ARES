namespace AlicatMFC.Commands.Requests;

internal class BasisHoldValvesClosedCommand : MfcCommand
{
  public BasisHoldValvesClosedCommand(char id, string firmware) : base(id, firmware)
  { }

  protected override string SerializeToString()
    => "HPUR 100";
}
