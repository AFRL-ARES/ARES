using AlicatMFCRemastered.Commands.Responses;
using AlicatMFCRemastered.Commands.Responses.Parsers;
using AlicatMFCRemastered.Commands.Responses.Streamed;

namespace AlicatMFCRemastered.Commands.Requests;

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
