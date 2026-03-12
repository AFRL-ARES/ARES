namespace ValveController.Interfaces;

public interface IValveController : IAsyncDisposable
{
  public Task EngageRelayOne();
  public Task EngageRelayTwo();
  public Task DisengageRelayOne();
  public Task DisengageRelayTwo();
  public Task<ValveController.Commands.Responses.RelayStatusResponse> GetRelayStatus();
  public Task EnableRelays();
}
