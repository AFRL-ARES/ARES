using AlicatMFC.Commands.Responses.Parsers;
using AlicatMFC.Commands.Responses.Streamed;
using Ares.Alicat.Mfc.Config;

namespace AlicatMFC.Commands.Requests;

internal class QueryGasStreamCommand : MfcCommandWithStreamedResponse<GasInfoEntry>
{
  private readonly MfcType _mfcType;

  public QueryGasStreamCommand(char id, string firmware, MfcType mfcType) : base(id, new GasInfoEntryParser(id), firmware)
  {
    _mfcType = mfcType;
  }

  protected override string SerializeToString()
    => _mfcType switch
    {
      MfcType.Normal => "??G*",
      MfcType.Basis2 => "GS *",
      _ => throw new ArgumentOutOfRangeException(nameof(MfcType))
    };
}
