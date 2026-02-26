using AlicatMFCRemastered.Commands.Responses;
using AlicatMFCRemastered.Commands.Responses.Parsers;
using AlicatMFCRemastered.Commands.Responses.Streamed;

namespace AlicatMFCRemastered.Commands.Requests;

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
