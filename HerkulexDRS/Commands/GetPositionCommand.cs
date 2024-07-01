using Ares.Device.Serial.Commands;
using HerkulexDRS.ResponseParsers;
using HerkulexDRS.Responses;

namespace HerkulexDRS.Commands;
public class GetPositionCommand : SerialCommandWithResponse<GetPositionResponse>
{
  public GetPositionCommand() : base(new GetPositionResponseParser())
  {
  }

  protected override byte[] Serialize()
  {
    return new byte[] { 0xFF, 0xFF, 0x09, 0x01, 0x04, 0x34, 0xCA, 0x3A, 0x02 };
  }
}
