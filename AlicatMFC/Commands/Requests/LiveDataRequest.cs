using AlicatMFC.Commands.Responses;
using AlicatMFC.Commands.Responses.Parsers;
using AlicatMFC.Commands.Responses.Streamed;

namespace AlicatMFC.Commands.Requests;

internal class LiveDataRequest : MfcCommandExpectingResponse<LiveDataResponse>
{
  public LiveDataRequest(DataFrameFormatEntry[] dataFrameEntries, string firmware) : base(dataFrameEntries[0].Id, new LiveDataParser(dataFrameEntries), firmware)
  {
  }

  protected override string SerializeToString() => string.Empty;
}
