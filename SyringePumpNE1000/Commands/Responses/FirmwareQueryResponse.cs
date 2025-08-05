using Ares.SyringePump.Ne1000.Messaging;

namespace SyringePumpNE1000.Commands.Responses;

internal class FirmwareQueryResponse : Response
{
  public FirmwareQueryResponse(int address, StatusPrompt status, string firmwareVersion) : base(address, status, null)
  {
    FirmwareVersion = firmwareVersion;
  }

  public string FirmwareVersion { get; }
}
