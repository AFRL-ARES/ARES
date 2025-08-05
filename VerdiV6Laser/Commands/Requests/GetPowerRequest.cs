using VerdiV6Laser.Commands.Responses;
using VerdiV6Laser.Commands.Responses.Parsers;

namespace VerdiV6Laser.Commands.Requests;

internal class GetPowerRequest : LaserCommandExpectingResponse<LaserPowerResponse>
{
  public GetPowerRequest() : base(new LaserPowerParser()) { }

  protected override string SerializeToString() => $"?SP\r\n";
}
