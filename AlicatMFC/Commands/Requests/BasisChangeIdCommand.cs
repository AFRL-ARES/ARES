using AlicatMFC.Commands.Responses;
using AlicatMFC.Commands.Responses.Parsers;
using AlicatMFC.Commands.Responses.Streamed;

namespace AlicatMFC.Commands.Requests;

internal class BasisChangeIdCommand : MfcCommandExpectingResponse<LiveDataResponse>
{
  public BasisChangeIdCommand(char currentId, char targetId, DataFrameFormatEntry[] dataFrames, string firmware) : base(currentId, new LiveDataParser(dataFrames), firmware)
  {
    TargetId = targetId;
  }

  private char TargetId { get; }

  protected override string SerializeToString()
    => $"@={TargetId}";
}
