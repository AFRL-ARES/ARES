using RestDevice.Services;

namespace UI.Backend.ViewModels.Devices.RestDevice;

public class RestDeviceUnitControlViewModel : UsbDeviceUnitViewModel
{
  private readonly RestDeviceRpc.RestDeviceRpcClient _client;

  public RestDeviceUnitControlViewModel(string deviceName, RestDeviceRpc.RestDeviceRpcClient client) : base(deviceName)
  {
    _client = client;
  }

  public async Task UpdateDeviceCapabilities()
  {
    var response = await _client.GetDeviceCapabilitiesAsync(new DeviceRequest() { DeviceName = DeviceName });
    DeviceMethods.Clear();
    MethodParameters.Clear();

    foreach(var method in response.DeviceMethods)
    {
      DeviceMethods.Add(method.MethodName, method.Parameters.ToList());
      method.Parameters.ToList().ForEach(p => MethodParameters.Add(p, string.Empty));
    }

    var blah = DeviceMethods.OrderBy(t => t.Value.Count);
  }

  public async Task HandleDeviceCommand(string commandName)
  {
    var found = DeviceMethods.TryGetValue(commandName, out var parameters);

    if(!found || parameters is null)
      throw new InvalidOperationException("Unknown method requested!");

    var request = new DeviceMethodRequest() { DeviceName = DeviceName, MethodName = commandName };

    foreach(var param in parameters)
    {
      var hasValue = MethodParameters.TryGetValue(param, out var value);

      if(hasValue)
      {
        request.ParameterValues.Add(value);
        request.ParameterNames.Add(param);
      }

      else
        throw new InvalidOperationException("Couldn't locate parameter!");
    }


    var response = await _client.CallDeviceMethodAsync(request);
  }

  public Dictionary<string, List<string>> DeviceMethods { get; set; } = new();

  public Dictionary<string, string> MethodParameters { get; set; } = new();
}
