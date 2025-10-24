using Ares.Alicat.Mfc.Messaging;

namespace AlicatMFC.Commands.Requests;

internal class SetSetpointSourceCommand : MfcCommand
{
  private readonly SetpointSource _source;

  public SetSetpointSourceCommand(char id, SetpointSource source, string firmware)
    : base(id, firmware)
  {
    _source = source;
  }

  protected override string SerializeToString()
  {
    var sourceCode = _source.ToStringSource();
    return $"LSS {sourceCode}";
  }
}
