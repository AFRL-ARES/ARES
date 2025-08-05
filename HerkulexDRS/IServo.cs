using Ares.Device.Serial;
using HerkulexDRS.Responses;

namespace HerkulexDRS;
public interface IServo : ISerialDevice<IServoConnection>, IAsyncDisposable
{
  public Task PistonDown();
  public Task PistonUp();
  public Task ResetServo();

  public Task<GetPositionResponse> GetPosition();

}
