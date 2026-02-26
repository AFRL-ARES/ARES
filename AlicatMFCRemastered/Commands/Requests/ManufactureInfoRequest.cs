using AlicatMFCRemastered.Commands.Requests;
using AlicatMFCRemastered.Commands.Responses;
using AlicatMFCRemastered.Commands.Responses.Parsers;

namespace AlicatMFCRemastered.Commands.Requests;

internal class ManufactureInfoRequest : MfcCommandExpectingResponse<ManufacturerInfoEntry>
{
  private readonly int? _lineNum;

  public ManufactureInfoRequest(char id, string firmware, int? lineNum = null) : base(id, new ManufactureInfoResponseEntryParser(id, lineNum), firmware)
  {
    _lineNum = lineNum;
  }

  protected override string SerializeToString()
    => $"??M{_lineNum ?? '*'}";
}
