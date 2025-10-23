using AlicatMFC.Commands.Responses;
using AlicatMFC.Commands.Responses.Parsers;
using AlicatMFC.Commands.Responses.Streamed;
using Ares.Alicat.Mfc.Config;
using UnitsNet;
using UnitsNet.Units;

namespace AlicatMFC.Commands.Requests;

internal class NewSetpointCommand : MfcCommandExpectingResponse<LiveDataResponse>
{
  private readonly StandardVolumeFlow _setpoint;
  private readonly StandardVolumeFlow _maxSetpoint;
  private readonly DataFrameFormatEntry[] _formatEntries;
  private readonly MfcType _mfcType;

  public NewSetpointCommand(char id, StandardVolumeFlow setpoint, DataFrameFormatEntry[] formatEntries, string firmware, MfcType mfcType) : base(id, new LiveDataParser(formatEntries), firmware)
  {
    _setpoint = setpoint;
    _formatEntries = formatEntries;
    _mfcType = mfcType;
    var setpointEntry = _formatEntries.FirstOrDefault(entry => entry.Field == DataFormatField.Setpoint);
    if (setpointEntry is not null && setpointEntry.Unit is not null)
    {
      _ = double.TryParse(setpointEntry.MaxVal, out var maxVal);
      _maxSetpoint = StandardVolumeFlow.From(maxVal, (StandardVolumeFlowUnit)setpointEntry.Unit);
    }
  }

  protected override string SerializeToString()
  {    
    if (_mfcType == MfcType.Basis2)
    {
      // BASIS2 devices just need the flow rate directly.
      return $"S {_setpoint.Value}";
    }

    var setpointFrac = _setpoint / _maxSetpoint;
    var counts = (int)Math.Round(setpointFrac * 64000, MidpointRounding.AwayFromZero);
    var commandData = $"{counts}";
    return commandData;
  }
}
