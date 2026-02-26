using AlicatMFCRemastered.Commands.Responses;
using AlicatMFCRemastered.Commands.Responses.Parsers;

namespace AlicatMFCRemastered.Commands.Requests;

internal class GenericLineRequest : MfcCommandExpectingResponse<GenericLineResponse>
{
  public GenericLineRequest(char id) : base(id, new GenericLineParser(id), string.Empty)
  {
  }

  protected override string SerializeToString()
    => string.Empty;
}
