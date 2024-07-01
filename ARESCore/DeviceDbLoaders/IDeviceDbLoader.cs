using System.Threading.Tasks;

namespace ARESCore.DeviceDbLoaders;

public interface IDeviceDbLoader
{
  Task Load();
}
