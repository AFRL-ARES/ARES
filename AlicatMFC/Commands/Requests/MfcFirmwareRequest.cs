using AlicatMFC.Commands.Responses;
using AlicatMFC.Commands.Responses.Parsers;

namespace AlicatMFC.Commands.Requests
{
  internal class MfcFirmwareRequest : MfcCommandExpectingResponse<FirmwareVersionResponse>
  {
    public MfcFirmwareRequest(char id) : base(id, new FirmwareVersionParser(id), String.Empty)
    {

    }

    protected override string SerializeToString()
      => $"VE";
  }
}
