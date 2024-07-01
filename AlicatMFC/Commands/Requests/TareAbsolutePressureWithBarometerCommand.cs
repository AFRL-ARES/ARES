using AlicatMFC.Commands.Responses;
using AlicatMFC.Commands.Responses.Parsers;
using AlicatMFC.Commands.Responses.Streamed;

namespace AlicatMFC.Commands.Requests;

internal class TareAbsolutePressureWithBarometerCommand : MfcCommandExpectingResponse<LiveDataResponse>
{
  public TareAbsolutePressureWithBarometerCommand(char id, DataFrameFormatEntry[] formatEntries, string firmware) : base(id, new LiveDataParser(formatEntries), firmware)
  {
  }

  protected override string SerializeToString()
    => $"PC";
}
