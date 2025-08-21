using Google.Protobuf;

namespace Ares.Core.Device;

public interface IDeviceConfigManager<in TConfig> where TConfig : IMessage, new()
{
  Task Add(string configId, string name, TConfig config);
  Task Remove(string configId);
  Task Update(string configId, TConfig config);
}
