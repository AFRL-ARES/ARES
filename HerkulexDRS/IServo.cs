using Ares.Device.Serial;
using HerkulexDRS.Responses;

namespace HerkulexDRS;
public interface IServo : ISerialDevice<IServoConnection>, IAsyncDisposable
{
  public void PistonDown();
  public void PistonUp();
  public void ResetServo();

  public Task<GetPositionResponse> GetPosition();

}
