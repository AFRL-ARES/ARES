using AlicatMFC.Commands.Responses;
using AlicatMFC.Commands.Responses.Parsers;
using Ares.Device.Serial.Commands;

namespace AlicatMFC.Commands.Requests;
internal class GetSetpointSourceCommand : MfcCommandExpectingResponse<SetpointSourceResponse>
{
  public GetSetpointSourceCommand(char id) : base(id, new SetpointSourceParser(id), ":)")
  {
  }

  protected override string SerializeToString()
    => "LSS";
}
