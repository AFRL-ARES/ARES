using VerdiV6Laser.Responses;
using VerdiV6Laser.Responses.Parsers;

namespace VerdiV6Laser.Commands;

public class GetPowerRequest : LaserCommandExpectingResponse<LaserPowerResponse>
{
  public GetPowerRequest() : base(new LaserPowerParser()) { }

  protected override string SerializeToString() => "?SP\r\n";
}
