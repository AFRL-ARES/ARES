using AlicatMFC.Commands.Responses;
using AlicatMFC.Commands.Responses.Parsers;
using AlicatMFC.Commands.Responses.Streamed;

namespace AlicatMFC.Commands.Requests;

internal class BasisHoldValvesAtCurrentPositionCommand : MfcCommandExpectingResponse<LiveDataResponse>
{
  private readonly double _currentValveDrive;

  public BasisHoldValvesAtCurrentPositionCommand(char id, string firmware, DataFrameFormatEntry[] dataFrames, double currentValveDrive) : base(id, new LiveDataParser(dataFrames), firmware)
  {
    _currentValveDrive = currentValveDrive;
  }

  protected override string SerializeToString()
    => $"HPUR {_currentValveDrive}";
}
