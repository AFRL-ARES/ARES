using AlicatMFC.Commands.Responses;
using AlicatMFC.Commands.Responses.Parsers;

namespace AlicatMFC.Commands.Requests;

internal class ChangeIdCommand : MfcCommandExpectingResponse<GenericLineResponse>
{
  public ChangeIdCommand(char currentId, char targetId, string firmware) : base(currentId, new GenericLineParser(targetId), firmware)
  {
    TargetId = targetId;
  }

  public char TargetId { get; }

  protected override string SerializeToString()
    => $"@={TargetId}";
}
