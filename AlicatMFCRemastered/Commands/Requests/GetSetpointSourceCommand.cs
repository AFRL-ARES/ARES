using AlicatMFCRemastered.Commands.Responses;
using AlicatMFCRemastered.Commands.Responses.Parsers;

namespace AlicatMFCRemastered.Commands.Requests;

internal class GetSetpointSourceCommand : MfcCommandExpectingResponse<SetpointSourceResponse>
{
  public GetSetpointSourceCommand(char id) : base(id, new SetpointSourceParser(id), ":)")
  {
  }

  protected override string SerializeToString()
    => "LSS";
}
