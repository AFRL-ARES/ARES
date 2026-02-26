using AlicatMFCRemastered.Commands.Responses;
using AlicatMFCRemastered.Commands.Responses.Parsers;
using AlicatMFCRemastered.Commands.Responses.Streamed;

namespace AlicatMFCRemastered.Commands.Requests;

internal class LiveDataRequest : MfcCommandExpectingResponse<LiveDataResponse>
{
  public LiveDataRequest(DataFrameFormatEntry[] dataFrameEntries, string firmware) : base(dataFrameEntries[0].Id, new LiveDataParser(dataFrameEntries), firmware)
  {
  }

  protected override string SerializeToString() => string.Empty;
}
