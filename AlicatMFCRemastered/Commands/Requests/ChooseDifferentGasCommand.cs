using AlicatMFCRemastered.Commands.Requests;
using AlicatMFCRemastered.Commands.Responses;
using AlicatMFCRemastered.Commands.Responses.Parsers;
using AlicatMFCRemastered.Commands.Responses.Streamed;
using AlicatMFCRemastered.Enums;

namespace AlicatMFCRemastered.Commands.Requests;

internal class ChooseDifferentGasCommand : MfcCommandExpectingResponse<LiveDataResponse>
{
  private readonly int _gasNumber;
  private readonly MfcTypeEnum _mfcType;

  public ChooseDifferentGasCommand(char id, int gasNumber, DataFrameFormatEntry[] formatEntries, string firmware, MfcTypeEnum mfcType) : base(id, new LiveDataParser(formatEntries), firmware)
  {
    _gasNumber = gasNumber;
    _mfcType = mfcType;
  }

  protected override string SerializeToString()
    => _mfcType switch
    {
      MfcTypeEnum.Normal => $"$$G{_gasNumber}",
      MfcTypeEnum.Basis2 => $"GS {_gasNumber}",
      _ => throw new ArgumentOutOfRangeException(nameof(_gasNumber)),
    };
}
