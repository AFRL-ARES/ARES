using Ares.Datamodel;
using Ares.Device.Rest;
using RestDevice.Structure;

namespace RestDevice;

public interface IRestDevice : IAresRestDevice, IAsyncDisposable
{
  List<RestDeviceMethod> Functions { get; set; }
  Task<AresValue> ProcessCommand(string cmdName, List<string> parameterNames, List<string> parameterValues);
}
