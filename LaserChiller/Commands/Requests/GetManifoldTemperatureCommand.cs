using LaserChiller.Commands.Responses;
using LaserChiller.Commands.Responses.Parsers;

namespace LaserChiller.Commands.Requests;

public class GetManifoldTemperatureCommand : ChillerCommandExpectingResponse<GetManifoldTemperatureResponse>
{
  public GetManifoldTemperatureCommand() : base(new ManifoldTemperatureParser()) { }

  protected override byte[] Serialize()
  {
    return new byte[] { 0x2E, 0x49, 0x37, 0x37, 0x0D };
  }
}
