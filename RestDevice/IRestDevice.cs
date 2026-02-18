using Ares.Datamodel;
using Ares.Device;
using RestDevice.Structure;

namespace RestDevice;

public interface IRestDevice : IAresDevice, IAsyncDisposable
{
  List<RestDeviceMethod> Functions { get; set; }
  Task<AresValue> ProcessCommand(string cmdName, List<string> parameterNames, List<string> parameterValues);
}
