using AlicatMFC.Commands.Responses;
using AlicatMFC.Commands.Responses.Parsers;
using AlicatMFC.Commands.Responses.Streamed;

namespace AlicatMFC.Commands.Requests;

internal class DeleteComposerMixCommand : MfcCommandExpectingResponse<LiveDataResponse>
{
  private readonly int _mixNumber;

  public DeleteComposerMixCommand(char id, int mixNumber, DataFrameFormatEntry[] formatEntries, string firmware) : base(id, new LiveDataParser(formatEntries), firmware)
  {
    _mixNumber = mixNumber;
  }

  protected override string SerializeToString()
    => $"GD {_mixNumber}";
}
