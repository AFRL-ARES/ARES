using AlicatMFC.Commands.Responses;
using AlicatMFC.Commands.Responses.Parsers;
using System;
using System.Linq;

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
