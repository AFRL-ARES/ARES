using AlicatMFC.Commands.Responses;
using AlicatMFC.Commands.Responses.Parsers;
using AlicatMFC.Commands.Responses.Streamed;

namespace AlicatMFC.Commands.Requests;

internal class BasisHoldValvesClosedCommand : MfcCommandExpectingResponse<LiveDataResponse>
{
  public BasisHoldValvesClosedCommand(char id, DataFrameFormatEntry[] dataFrames, string firmware) : base(id, new LiveDataParser(dataFrames), firmware)
  { }

  protected override string SerializeToString()
    => "HPUR 0";
}
