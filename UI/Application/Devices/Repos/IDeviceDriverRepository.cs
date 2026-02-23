namespace UI.Application.Devices.Repos;

public interface IDeviceDriverRepository
{
    IReadOnlyList<string> DriverNames { get; }
    void Update(IEnumerable<string> driverNames);
    IObservable<IReadOnlyList<string>> DriverNamesChanged { get; }
}
