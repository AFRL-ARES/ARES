using AlicatMFCRemastered.Commands.Extensions;
using AlicatMFCRemastered.Commands.Responses;
using AlicatMFCRemastered.Commands.Responses.Parsers;
using AlicatMFCRemastered.Enums;

namespace AlicatMFCRemastered.Commands.Requests;

internal class SetSetpointSourceCommand : MfcCommandExpectingResponse<SetpointSourceResponse>
{
  private readonly MfcSetpointSourceEnum _source;

  public SetSetpointSourceCommand(char id, MfcSetpointSourceEnum source, string firmware)
    : base(id, new SetpointSourceParser(id), firmware)
  {
    _source = source;
  }

  protected override string SerializeToString()
  {
    var sourceCode = _source.ToStringSource();
    return $"LSS {sourceCode}";
  }
}
