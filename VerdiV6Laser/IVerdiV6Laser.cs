namespace VerdiV6Laser;

public interface IVerdiV6Laser : IAsyncDisposable
{
  Task ActivateLaser();
  Task DeactivateLaser();
  Task SetLaserPower(double desiredPower);
  Task SetLaserShutter(bool shutter);
  Task<bool> GetLaserShutter();
  Task<double> GetLaserPower();
  double CurrentPower { get; }
  double DesiredPower { get; }
  bool Shutter { get; }
}
