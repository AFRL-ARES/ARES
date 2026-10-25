using DynamicData;
using UI.Infrastructure.Devices.Remote;

namespace UI.Application.Devices.Repos;

public interface IRemoteDeviceAdapterRepository : ISourceCache<RemoteDeviceAdapter, string>, IDisposable
{
}