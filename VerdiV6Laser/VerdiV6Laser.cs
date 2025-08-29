using Ares.Device.Serial;
using VerdiV6Laser.Commands;
using VerdiV6Laser.Commands.Requests;

namespace VerdiV6Laser
{
  public class VerdiV6Laser : SerialDevice<ILaserConnection>, IVerdiV6Laser
  {
    public VerdiV6Laser(string name, ILaserConnection connection) : base(name, connection)
    {
    }

    public async Task<double> GetLaserPower()
    {
      var request = new GetPowerRequest();

      try
      {
        var response = await Send(request);
        CurrentPower = response.Power;
      }

      catch(OperationCanceledException)
      {
        //Do Nothing, bubble up notification?
      }

      catch(TimeoutException)
      {
        //Do Nothing, bubble up notification?
      }

      return CurrentPower;
    }

    public async Task<bool> GetLaserShutter()
    {
      var request = new GetShutterRequest();

      try
      {
        var response = await Send(request);
        Shutter = response.Shutter;
      }

      catch(OperationCanceledException)
      {
        //Do Nothing, bubble up notification?
      }

      catch(TimeoutException)
      {
        //Do Nothing, bubble up notification?
      }

      return Shutter;
    }

    public async Task ActivateLaser()
    {
      var request = new SetPowerRequest(DesiredPower);
      await Connection.Send(request);
      await GetLaserPower();
    }

    public async Task DeactivateLaser()
    {
      var request = new SetPowerRequest(0.1);
      await Connection.Send(request);
      await GetLaserPower();
    }

    public async Task SetLaserPower(double desiredPower)
    {
      DesiredPower = desiredPower;
      await GetLaserPower();
    }

    public async Task SetLaserShutter(bool shutter)
    {
      var request = new SetShutterRequest(shutter);
      await Connection.Send(request);
      await GetLaserShutter();
    }

    public ValueTask DisposeAsync()
    {
      return new ValueTask();
    }

    protected override Task<SerialDeviceValidationResult> Validate()
    {
      //Not sure we have a way to validate the laser?
      return Task.FromResult(new SerialDeviceValidationResult(true));
    }

    private Task<T> Send<T>(LaserCommandExpectingResponse<T> command) where T : CommandResponse
    {
      return Connection.Send(command, TimeSpan.FromSeconds(5));
    }

    public override Task EnterSafeMode(CancellationToken ct)
    {
      //TODO: IMPLEMENT ME!!!
      throw new NotImplementedException();
    }

    public double CurrentPower { get; set; } = 0.01;
    public double DesiredPower { get; set; }
    public bool Shutter { get; set; } = false;
    public Guid Id { get; } = Guid.NewGuid();

  }
}
