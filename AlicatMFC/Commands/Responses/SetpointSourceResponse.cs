using Ares.Alicat.Mfc.Messaging;

namespace AlicatMFC.Commands.Responses;

public class SetpointSourceResponse : CommandResponse
{
  public SetpointSource Source { get; }

  public SetpointSourceResponse(char id, SetpointSource source) : base(id)
  {
    Source = source;
  }
}