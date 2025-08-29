using Ares.Device.Serial;
using HerkulexDRS.Commands;
using HerkulexDRS.Responses;

namespace HerkulexDRS;
public class Servo : SerialDevice<IServoConnection>, IServo
{
  public Servo(string name, IServoConnection connection) : base(name, connection)
  {

  }

  public async Task PistonDown()
  {
    PistonRaised = false;
    await Connection.Send(new PistonDownCommand());
  }

  public async Task PistonUp()
  {
    PistonRaised = true;
    await Connection.Send(new PistonUpCommand());
  }

  public async Task ResetServo()
  {
    await Connection.Send(new RebootCommand());
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

  protected override async Task<SerialDeviceValidationResult> Validate()
  {
    try
    {
      await Connection.Send(new RebootCommand());
      return new SerialDeviceValidationResult(true);
    }
    catch(Exception e)
    {
      return new SerialDeviceValidationResult(false, e.Message);
    }
  }

  public override async Task EnterSafeMode(CancellationToken ct)
  {
    //Disengage Servo
    await PistonDown();
  }

  public bool PistonRaised { get; set; } = false;
}
