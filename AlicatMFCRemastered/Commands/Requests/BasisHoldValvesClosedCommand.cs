using AlicatMFCRemastered.Commands.Responses;
using AlicatMFCRemastered.Commands.Responses.Parsers;
using AlicatMFCRemastered.Commands.Responses.Streamed;

namespace AlicatMFCRemastered.Commands.Requests;

internal class BasisHoldValvesClosedCommand : MfcCommandExpectingResponse<LiveDataResponse>
{
  public BasisHoldValvesClosedCommand(char id, DataFrameFormatEntry[] dataFrames, string firmware) : base(id, new LiveDataParser(dataFrames), firmware)
  { }

  protected override string SerializeToString()
    => "HPUR 0";
}
