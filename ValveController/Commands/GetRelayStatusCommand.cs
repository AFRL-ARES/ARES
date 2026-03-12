using Ares.Toolkit.Serial.Commands;
using ValveController.Commands.Responses;
using ValveController.Commands.Responses.Parsers;

namespace ValveController.Commands;
public class GetRelayStatusCommand : SerialCommandWithResponse<RelayStatusResponse>
{
  public GetRelayStatusCommand() : base(new RelayStatusResponseParser())
  {
  }

  protected override byte[] Serialize()
  {
    return new byte[] { 7 };
  }
}
