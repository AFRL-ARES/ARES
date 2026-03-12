using HerkulexDRS.Responses;

namespace HerkulexDRS;
public interface IServo : IAsyncDisposable
{
  public Task PistonDown();
  public Task PistonUp();
  public Task ResetServo();
  public Task<GetPositionResponse> GetPosition();
}
