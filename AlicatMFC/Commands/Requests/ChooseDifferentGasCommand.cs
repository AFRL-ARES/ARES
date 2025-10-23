using AlicatMFC.Commands.Responses;
using AlicatMFC.Commands.Responses.Parsers;
using AlicatMFC.Commands.Responses.Streamed;
using Ares.Alicat.Mfc.Config;

namespace AlicatMFC.Commands.Requests;

internal class ChooseDifferentGasCommand : MfcCommandExpectingResponse<LiveDataResponse>
{
  private readonly int _gasNumber;
  private readonly MfcType _mfcType;

  public ChooseDifferentGasCommand(char id, int gasNumber, DataFrameFormatEntry[] formatEntries, string firmware, MfcType mfcType) : base(id, new LiveDataParser(formatEntries), firmware)
  {
    _gasNumber = gasNumber;
    _mfcType = mfcType;
  }

  protected override string SerializeToString()
    => _mfcType switch
    {
      MfcType.Normal => $"$$G{_gasNumber}",
      MfcType.Basis2 => $"GS {_gasNumber}",
      _ => throw new ArgumentOutOfRangeException(nameof(_gasNumber)),
    };
}
