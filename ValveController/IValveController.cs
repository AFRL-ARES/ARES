using Ares.Device.Serial;
using ValveController.Commands.Responses;

namespace ValveController;
public interface IValveController : ISerialDevice<IValveControllerConnection>, IAsyncDisposable
{
  public void EngageRelayOne();
  public void EngageRelayTwo();
  public void DisengageRelayOne();
  public void DisengageRelayTwo();
  public Task<RelayStatusResponse> GetRelayStatus();
  public void EnableRelays();
}
