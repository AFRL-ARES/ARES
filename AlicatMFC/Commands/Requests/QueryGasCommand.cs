using AlicatMFC.Commands.Responses.Parsers;
using AlicatMFC.Commands.Responses.Streamed;

namespace AlicatMFC.Commands.Requests;

internal class QueryGasCommand : MfcCommandExpectingResponse<GasInfoEntry>
{
  public QueryGasCommand(char id, string firmware, int? lineNum = null) : base(id, new GasInfoEntryParser(id, lineNum), firmware)
  {
    LineNum = lineNum;
  }

  public int? LineNum { get; set; }

  protected override string SerializeToString()
    => $"??G{LineNum ?? '*'}";
}
