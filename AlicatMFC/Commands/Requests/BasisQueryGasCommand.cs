using AlicatMFC.Commands.Responses;
using AlicatMFC.Commands.Responses.Parsers;
using AlicatMFC.Commands.Responses.Streamed;
using Ares.Alicat.Mfc.Config;

namespace AlicatMFC.Commands.Requests;

internal class BasisQueryGasCommand : MfcCommandExpectingResponse<GasInfoEntryList>
{
  public BasisQueryGasCommand(char id, string firmware) : base(id, new GasInfoListParser(id), firmware)
  {
  }

  protected override string SerializeToString()
    => "GS *";
}
