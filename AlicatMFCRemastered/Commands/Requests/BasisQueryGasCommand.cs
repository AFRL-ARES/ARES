using AlicatMFCRemastered.Commands.Responses;
using AlicatMFCRemastered.Commands.Responses.Parsers;

namespace AlicatMFCRemastered.Commands.Requests;

internal class BasisQueryGasCommand : MfcCommandExpectingResponse<GasInfoEntryList>
{
  public BasisQueryGasCommand(char id, string firmware) : base(id, new GasInfoListParser(id), firmware)
  {
  }

  protected override string SerializeToString()
    => "GS *";
}
