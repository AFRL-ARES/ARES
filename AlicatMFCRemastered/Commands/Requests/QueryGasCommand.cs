using AlicatMFCRemastered.Commands.Responses.Parsers;
using AlicatMFCRemastered.Commands.Responses.Streamed;
using Ares.Alicat.Mfc.Config;

namespace AlicatMFC.Commands.Requests;

internal class QueryGasCommand : MfcCommandExpectingResponse<GasInfoEntry>
{
  private readonly MfcType _mfcType;

  public QueryGasCommand(char id, string firmware, MfcType mfcType, int? lineNum = null) : base(id, new GasInfoEntryParser(id, lineNum), firmware)
  {
    _mfcType = mfcType;
    LineNum = lineNum;
  }

  public int? LineNum { get; set; }

  protected override string SerializeToString()
    => _mfcType switch {
      MfcType.Normal => $"??G{LineNum ?? '*'}",
      MfcType.Basis2 => $"GS *",
      _ => throw new ArgumentOutOfRangeException(nameof(MfcType))
    };
}
