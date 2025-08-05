using Ares.Device.Serial;
using GenericSerialDevice.Commands.Responses;
using RestSerialDevice.Services;

namespace RestSerialDevice;

public interface ISerialRestDevice : ISerialDevice<ISerialRestDeviceConnection>, IAsyncDisposable
{
  IObservable<ReadDataResponse?> StateStream { get; }
  Task<ReadDataResponse> GetAndUpdateState();
  public List<KeyValuePair<string, string>> Data { get; }
  public string DeviceId { get; }
  public string Hardware { get; }
  public bool IsExternalDeviceConnected { get; }
  
  /// <summary>
  /// Processes a command received from a gRPC call.
  /// </summary>
  Task<DeviceMethodResponse> ProcessCommand(
    string methodName,
    List<string> parameterNames,
    List<string> parameterValues);

  /// <summary>
  /// Gets the capabilities (functions/methods) exposed by the device.
  /// </summary>
  IEnumerable<RestSerialDevice.Structure.RestDeviceMethod> Functions { get; set; } // Or List<DeviceMethodInfo> if preferred
}
