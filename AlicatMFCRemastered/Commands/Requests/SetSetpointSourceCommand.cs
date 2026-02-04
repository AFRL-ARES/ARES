using AlicatMFCRemastered.Commands.Responses;
using AlicatMFCRemastered.Commands.Responses.Parsers;

namespace AlicatMFC.Commands.Requests;

internal class SetSetpointSourceCommand : MfcCommandExpectingResponse<SetpointSourceResponse>
{
  private readonly Ares.Alicat.Mfc.Messaging.SetpointSource _source;

  public SetSetpointSourceCommand(char id, Ares.Alicat.Mfc.Messaging.SetpointSource source, string firmware)
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
