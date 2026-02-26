using AlicatMFCRemastered.Commands.Responses;
using AlicatMFCRemastered.Commands.Responses.Parsers;
using AlicatMFCRemastered.Commands.Responses.Streamed;
using UnitsNet;

namespace AlicatMFCRemastered.Commands.Requests;

internal class BasisNewSetpointCommand : MfcCommandExpectingResponse<LiveDataResponse>
{
  private readonly StandardVolumeFlow _setpoint;

  public BasisNewSetpointCommand(char id, StandardVolumeFlow setpoint, DataFrameFormatEntry[] dataFrames, string firmware) : base(id, new LiveDataParser(dataFrames), firmware)
  {
    _setpoint = setpoint;
  }

  protected override string SerializeToString()
  {
    return $"S {_setpoint.Value}";
  }
}
