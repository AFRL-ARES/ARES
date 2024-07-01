using Ares.Device.Serial.Commands;
using HerkulexDRS.ResponseParsers;
using HerkulexDRS.Responses;

namespace HerkulexDRS.Commands;
internal class StatusCommand : SerialCommandWithResponse<StatusResponse>
{
  public StatusCommand() : base(new StatusResponseParser())
  {

  }

  protected override byte[] Serialize()
  {
    return new byte[] { 0xFF, 0xFF, 0x07, 0x01, 0x07, 0x00, 0xFE };
  }
}
