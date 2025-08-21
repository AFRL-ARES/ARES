

/*
using RestSerialDevice.Services;
using System.Linq;


namespace UI.Backend.ViewModels.Devices.SerialRestDevice;

public class SerialRestDeviceUnitControlViewModel : UsbDeviceUnitViewModel
{
    private readonly RestSerialDeviceRpc.RestSerialDeviceRpcClient _client;

    public SerialRestDeviceUnitControlViewModel(string deviceName, RestSerialDeviceRpc.RestSerialDeviceRpcClient client) :
        base(deviceName)
    {
        _client = client;
    }

    public async Task UpdateDeviceCapabilities()
    {
        
        var response = await _client.GetDeviceCapabilitiesAsync(new DeviceRequest() { DeviceId = DeviceId });
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

        if (!found || parameters is null)
            throw new InvalidOperationException("Unknown method requested!");
        var request = new DeviceMethodRequest() { DeviceId = DeviceId, MethodName = commandName };

        foreach (var param in parameters)
        {
            var hasValue = MethodParameters.TryGetValue(param, out var value);

            if (hasValue)
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
*/



using RestSerialDevice.Services;

namespace UI.Backend.ViewModels.Devices.SerialRestDevice;

public class SerialRestDeviceUnitControlViewModel : SerialDeviceUnitViewModel, IAsyncDisposable
{
    private readonly RestSerialDeviceRpc.RestSerialDeviceRpcClient _client;
    private readonly CancellationTokenSource _stateStreamCts = new();
    private Task _stateListener = Task.CompletedTask;

    public SerialRestDeviceUnitControlViewModel(string deviceId, string deviceName, RestSerialDeviceRpc.RestSerialDeviceRpcClient client) :
        base(deviceId, deviceName)
    {
        _client = client;
    }

    public async Task UpdateDeviceCapabilities()
    {
        
        var response = await _client.GetDeviceCapabilitiesAsync(new DeviceRequest() { DeviceId = DeviceId });
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

        if (!found || parameters is null)
            throw new InvalidOperationException("Unknown method requested!");
        var request = new DeviceMethodRequest() { DeviceId = DeviceId, MethodName = commandName };

        foreach (var param in parameters)
        {
            var hasValue = MethodParameters.TryGetValue(param, out var value);

            if (hasValue)
            {
                request.ParameterValues.Add(value);
                request.ParameterNames.Add(param);
            }
            else 
                throw new InvalidOperationException("Couldn't locate parameter!");
            
        }

        var response = await _client.CallDeviceMethodAsync(request);
    }
    
    public async ValueTask DisposeAsync()
    {
        _stateStreamCts.Cancel();
        await _stateListener;
        _stateStreamCts.Dispose();

        GC.SuppressFinalize(this);
    }

    public Dictionary<string, List<string>> DeviceMethods { get; set; } = new();
    public Dictionary<string, string> MethodParameters { get; set; } = new();
}


