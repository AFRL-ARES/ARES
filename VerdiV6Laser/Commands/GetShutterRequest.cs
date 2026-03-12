using VerdiV6Laser.Responses;
using VerdiV6Laser.Responses.Parsers;

namespace VerdiV6Laser.Commands;

public class GetShutterRequest : LaserCommandExpectingResponse<LaserShutterResponse>
{
  public GetShutterRequest() : base(new LaserShutterParser()) { }

  protected override string SerializeToString() => "?S\r\n";
}
