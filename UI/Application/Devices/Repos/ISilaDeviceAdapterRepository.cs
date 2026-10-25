using DynamicData;
using UI.Infrastructure.Devices.Sila;

namespace UI.Application.Devices.Repos;

public interface ISilaDeviceAdapterRepository : ISourceCache<SilaDeviceAdapter, string>, IDisposable
{
}
