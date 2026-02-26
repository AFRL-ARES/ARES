using AlicatMFCRemastered.Enums;

namespace AlicatMFCRemastered.Commands.Responses;

public class SetpointSourceResponse : CommandResponse
{
  public MfcSetpointSourceEnum Source { get; }

  public SetpointSourceResponse(char id, MfcSetpointSourceEnum source) : base(id)
  {
    Source = source;
  }
}