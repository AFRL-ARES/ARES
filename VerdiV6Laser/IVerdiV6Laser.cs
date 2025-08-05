using Ares.Device.Serial;

namespace VerdiV6Laser
{
  public interface IVerdiV6Laser : ISerialDevice<ILaserConnection>, IAsyncDisposable
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
}
