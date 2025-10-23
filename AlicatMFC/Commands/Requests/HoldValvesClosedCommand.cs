using AlicatMFC.Commands.Responses;
using AlicatMFC.Commands.Responses.Parsers;
using AlicatMFC.Commands.Responses.Streamed;
using Ares.Alicat.Mfc.Config;

namespace AlicatMFC.Commands.Requests;

internal class HoldValvesClosedCommand : MfcCommandExpectingResponse<LiveDataResponse>
{
  private readonly MfcType _mfcType;

  public HoldValvesClosedCommand(char id, DataFrameFormatEntry[] formatEntries, string firmware, MfcType mfcType) : base(id, new LiveDataParser(formatEntries), firmware)
  {
    _mfcType = mfcType;
  }

  protected override string SerializeToString()
    => _mfcType == MfcType.Normal ? "HC" : "HPUR 100";
}
