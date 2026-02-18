using DynamicData;

namespace UI.Application.Devices.Repos;

public interface IDeviceAdapterRepository : ISourceCache<IAresDeviceAdapter, string>, IDisposable
{
}