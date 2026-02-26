using AlicatMFCRemastered.Commands.Requests;
using AlicatMFCRemastered.Commands.Responses;
using AlicatMFCRemastered.Commands.Responses.Parsers;
using AlicatMFCRemastered.Commands.Responses.Streamed;

namespace AlicatMFCRemastered.Commands.Requests;

internal class CancelValveHoldCommand : MfcCommandExpectingResponse<LiveDataResponse>
{
  public CancelValveHoldCommand(char id, DataFrameFormatEntry[] formatEntries, string firmware) : base(id, new LiveDataParser(formatEntries), firmware)
  {
  }

  protected override string SerializeToString()
    => $"C";
}
