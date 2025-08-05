using Ares.Device.Serial.Commands;
using HerkulexDRS.Responses;
using HerkulexDRS.Responses.Parsers;

namespace HerkulexDRS.Commands;
internal class StatusCommand : SerialCommandWithResponse<StatusResponse>
{
  public StatusCommand() : base(new StatusResponseParser())
  {

  }

  protected override byte[] Serialize()
  {
    return new byte[] { 0xFF, 0xFF, 0x07, 0xFE, 0x07, 0xFE, 0x00 };
  }
}
