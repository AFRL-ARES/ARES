using System.Threading;
using System.Threading.Tasks;

namespace Ares.Core.Device.Drivers.Loading;

public interface IDeviceDriverLoader
{
  Task LoadModulesAsync(string directoryPath, CancellationToken ct = default);
  Task<DeviceDriver> LoadAsync(string aresFilePath, CancellationToken ct = default);
  Task<DeviceDriver> LoadFromDirectoryAsync(string moduleDirectory, CancellationToken ct = default);
}
