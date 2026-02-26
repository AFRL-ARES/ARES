using AlicatMFCRemastered.Commands.Responses;
using AlicatMFCRemastered.Commands.Responses.Parsers;
using AlicatMFCRemastered.Commands.Responses.Streamed;
using AlicatMFCRemastered.Models;

namespace AlicatMFCRemastered.Commands.Requests;

internal class NewComposerMixCommand : MfcCommandExpectingResponse<LiveDataResponse>
{
  private readonly MfcGasComposition _composerMix;

  public NewComposerMixCommand(char id, MfcGasComposition composerMix, DataFrameFormatEntry[] formatEntries, string firmware) : base(id, new LiveDataParser(formatEntries), firmware)
  {
    _composerMix = composerMix;
  }

  protected override string SerializeToString()
  {
    var composition =
      _composerMix
        .Entries
        .Select(entry => $"{entry.Percentage} {entry.GasNumber}\r")
        .Aggregate((left, right) => $"{left} {right}\r");

    return $"GM {_composerMix.Name} {_composerMix.Number} {composition}";
  }
}
