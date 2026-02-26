using AlicatMFCRemastered.Commands.Responses;
using AlicatMFCRemastered.Commands.Responses.Parsers;

namespace AlicatMFCRemastered.Commands.Requests
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
