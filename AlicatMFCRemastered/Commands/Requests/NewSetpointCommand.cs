using AlicatMFCRemastered.Commands.Responses;
using AlicatMFCRemastered.Commands.Responses.Parsers;
using AlicatMFCRemastered.Commands.Responses.Streamed;
using UnitsNet;
using UnitsNet.Units;

namespace AlicatMFCRemastered.Commands.Requests;

internal class NewSetpointCommand : MfcCommandExpectingResponse<LiveDataResponse>
{
  private readonly StandardVolumeFlow _setpoint;
  private readonly StandardVolumeFlow _maxSetpoint;
  private readonly DataFrameFormatEntry[] _formatEntries;

  public NewSetpointCommand(char id, StandardVolumeFlow setpoint, DataFrameFormatEntry[] formatEntries, string firmware) : base(id, new LiveDataParser(formatEntries), firmware)
  {
    _setpoint = setpoint;
    _formatEntries = formatEntries;
    var setpointEntry = _formatEntries.FirstOrDefault(entry => entry.Field == DataFormatField.Setpoint);
    if(setpointEntry is not null && setpointEntry.Unit is not null)
    {
      _ = double.TryParse(setpointEntry.MaxVal, out var maxVal);
      _maxSetpoint = StandardVolumeFlow.From(maxVal, (StandardVolumeFlowUnit)setpointEntry.Unit);
    }
  }

  protected override string SerializeToString()
  {
    var setpointFrac = _setpoint / _maxSetpoint;
    var counts = (int)Math.Round(setpointFrac * 64000, MidpointRounding.AwayFromZero);
    var commandData = $"{counts}";
    return commandData;
  }
}
