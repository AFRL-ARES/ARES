using AlicatMFC.Commands.Responses.Parsers;
using AlicatMFC.Commands.Responses.Streamed;

namespace AlicatMFC.Commands.Requests;

internal class DataFormatRequest : MfcCommandExpectingResponse<DataFrameFormatEntry>
{
  public DataFormatRequest(char id, string firmware, int? lineNum = null) : base(id, new DataFormatEntryParser(id, lineNum), firmware)
  {
    LineNum = lineNum;
  }
  public int? LineNum { get; set; }

  protected override string SerializeToString() => $"??D{LineNum ?? '*'}";
}
