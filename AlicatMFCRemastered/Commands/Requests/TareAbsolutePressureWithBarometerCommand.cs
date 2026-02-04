using AlicatMFCRemastered.Commands.Responses;
using AlicatMFCRemastered.Commands.Responses.Parsers;
using AlicatMFCRemastered.Commands.Responses.Streamed;

namespace AlicatMFC.Commands.Requests;

internal class TareAbsolutePressureWithBarometerCommand : MfcCommandExpectingResponse<LiveDataResponse>
{
  public TareAbsolutePressureWithBarometerCommand(char id, DataFrameFormatEntry[] formatEntries, string firmware) : base(id, new LiveDataParser(formatEntries), firmware)
  {
  }

  protected override string SerializeToString()
    => $"PC";
}
