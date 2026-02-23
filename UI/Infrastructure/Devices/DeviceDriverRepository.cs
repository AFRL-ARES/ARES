using System.Reactive.Subjects;
using UI.Application.Devices.Repos;

namespace UI.Infrastructure.Devices;

public class DeviceDriverRepository : IDeviceDriverRepository
{
  private readonly BehaviorSubject<IReadOnlyList<string>> _driverNamesSubject = new(new List<string>());

  public IReadOnlyList<string> DriverNames => _driverNamesSubject.Value;

  public void Update(IEnumerable<string> driverNames)
  {
    _driverNamesSubject.OnNext(driverNames.ToList().AsReadOnly());
  }

  public IObservable<IReadOnlyList<string>> DriverNamesChanged => _driverNamesSubject;
}
