using AlicatMFCRemastered.Commands.Responses.Parsers;
using AlicatMFCRemastered.Commands.Responses.Streamed;
using AlicatMFCRemastered.Enums;

namespace AlicatMFCRemastered.Commands.Requests;

internal class QueryGasCommand : MfcCommandExpectingResponse<GasInfoEntry>
{
  private readonly MfcTypeEnum _mfcType;

  public QueryGasCommand(char id, string firmware, MfcTypeEnum mfcType, int? lineNum = null) : base(id, new GasInfoEntryParser(id, lineNum), firmware)
  {
    _mfcType = mfcType;
    LineNum = lineNum;
  }

  public int? LineNum { get; set; }

  protected override string SerializeToString()
    => _mfcType switch {
      MfcTypeEnum.Normal => $"??G{LineNum ?? '*'}",
      MfcTypeEnum.Basis2 => $"GS *",
      _ => throw new ArgumentOutOfRangeException(nameof(MfcTypeEnum))
    };
}
