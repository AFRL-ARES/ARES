namespace AlicatMFCRemastered.Commands.Responses.Streamed;

public class GasInfoEntry : CommandResponse
{
  public GasInfoEntry(char id, string gas, uint index) : base(id)
  {
    Gas = gas;
    Index = index;
  }

  public GasInfoEntry(char id) : base(id)
  {
    IsEndMarker = true;
    Gas = string.Empty;
  }

  public bool IsEndMarker { get; }
  public string Gas { get; }
  public uint Index { get; }
}
