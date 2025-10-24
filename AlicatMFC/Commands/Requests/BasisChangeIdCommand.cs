using AlicatMFC.Commands.Responses;
using AlicatMFC.Commands.Responses.Parsers;

namespace AlicatMFC.Commands.Requests;

internal class BasisChangeIdCommand : MfcCommand
{
  public BasisChangeIdCommand(char currentId, char targetId, string firmware) : base(currentId, firmware)
  {
    TargetId = targetId;
  }

  private char TargetId { get; }

  protected override string SerializeToString()
    => $"@={TargetId}";
}
