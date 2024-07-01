using AlicatMFC.Commands.Responses;
using AlicatMFC.Commands.Responses.Parsers;
using AlicatMFC.Commands.Responses.Streamed;

namespace AlicatMFC.Commands.Requests;

internal class ChooseDifferentGasCommand : MfcCommandExpectingResponse<LiveDataResponse>
{
  private readonly int _gasNumber;

  public ChooseDifferentGasCommand(char id, int gasNumber, DataFrameFormatEntry[] formatEntries, string firmware) : base(id, new LiveDataParser(formatEntries), firmware)
  {
    _gasNumber = gasNumber;
  }

  protected override string SerializeToString()
    => $"$$G{_gasNumber}";
}
