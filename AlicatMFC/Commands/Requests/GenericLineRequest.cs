using AlicatMFC.Commands.Responses;
using AlicatMFC.Commands.Responses.Parsers;

namespace AlicatMFC.Commands.Requests;

internal class GenericLineRequest : MfcCommandExpectingResponse<GenericLineResponse>
{
  public GenericLineRequest(char id) : base(id, new GenericLineParser(id), string.Empty)
  {
  }

  protected override string SerializeToString()
    => string.Empty;
}
