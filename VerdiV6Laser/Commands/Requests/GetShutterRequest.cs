using VerdiV6Laser.Commands.Responses;
using VerdiV6Laser.Commands.Responses.Parsers;

namespace VerdiV6Laser.Commands.Requests
{
  internal class GetShutterRequest : LaserCommandExpectingResponse<LaserShutterResponse>
  {
    public GetShutterRequest() : base(new LaserShutterParser()) { }

    protected override string SerializeToString() => $"?S\r\n";
  }
}
