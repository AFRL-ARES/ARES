using Ares.Device.Serial;
using ValveController.Commands.Responses;

namespace ValveController;
public interface IValveController : ISerialDevice<IValveControllerConnection>, IAsyncDisposable
{
  public Task EngageRelayOne();
  public Task EngageRelayTwo();
  public Task DisengageRelayOne();
  public Task DisengageRelayTwo();
  public Task<RelayStatusResponse> GetRelayStatus();
  public Task EnableRelays();
}
