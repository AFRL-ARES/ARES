using System;
using System.Linq;

namespace AlicatMFC.Commands.Responses
{
  internal class FirmwareVersionResponse : CommandResponse
  {
    public FirmwareVersionResponse(char id, string firmwareVersion) : base(id)
    {
      FirmwareVersion = firmwareVersion;
    }

    public string FirmwareVersion { get; }
  }
}
