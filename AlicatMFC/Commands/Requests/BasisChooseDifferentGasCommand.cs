using AlicatMFC.Commands.Responses.Parsers;
using AlicatMFC.Commands.Responses.Streamed;

namespace AlicatMFC.Commands.Requests;

internal class BasisChooseDifferentGasCommand : MfcCommandExpectingResponse<GasInfoEntry>
{
  private readonly int _gasNumber;

  public BasisChooseDifferentGasCommand(char id, int gasNumber, string firmware) : base(id, new GasInfoEntryParser(id), firmware)
  {
    _gasNumber = gasNumber;
  }

  protected override string SerializeToString()
    => $"GS {_gasNumber}";
}
