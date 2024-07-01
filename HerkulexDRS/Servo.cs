using Ares.Device.Serial;
using HerkulexDRS.Commands;
using HerkulexDRS.Responses;

namespace HerkulexDRS;
public class Servo : SerialDevice<IServoConnection>, IServo
{

  public Servo(string name, IServoConnection connection) : base(name, connection)
  {

  }

  public void PistonDown()
  {
    PistonRaised = false;
    Connection.Send(new PistonDownCommand());
  }

  public void PistonUp()
  {
    PistonRaised = true;
    Connection.Send(new PistonUpCommand());
  }

  public void ResetServo()
  {
    Connection.Send(new RebootCommand());
  }

  public async Task<GetPositionResponse> GetPosition()
  {
    var response = await Connection.Send(new GetPositionCommand());
    return response;
  }

  public ValueTask DisposeAsync()
  {
    return ValueTask.CompletedTask;
  }

  protected override async Task<DeviceValidationResult> Validate()
  {
    try
    {
      await Connection.Send(new PistonDownCommand());
      return new DeviceValidationResult(true);
    }
    catch (Exception e)
    {
      return new DeviceValidationResult(false, e.Message);
    }
  }

  public bool PistonRaised { get; set; } = false;
}
