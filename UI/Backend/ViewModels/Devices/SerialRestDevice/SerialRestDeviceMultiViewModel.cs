/*
using Ares.Messaging.Device;
using Google.Protobuf.WellKnownTypes;
using RestSerialDevice.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UI.Backend.ViewModels.Devices.SerialRestDevice;

public class SerialRestDeviceMultiViewModel : UsbDeviceConnectorViewModel<SerialRestDeviceUnitControlViewModel>
{
    private readonly RestSerialDeviceRpc.RestSerialDeviceRpcClient _client;

    public SerialRestDeviceMultiViewModel(RestSerialDeviceRpc.RestSerialDeviceRpcClient client,
        AresDevices.AresDevicesClient devicesClient) : base(devicesClient)
    {
        _client = client;
    }

    protected override SerialRestDeviceUnitControlViewModel CreateUnitVm(string deviceName) => new(deviceName, _client);

    protected override async Task<IEnumerable<string>> GetDeviceNames()
    {
        var devicesResponse = await _client.GetAllGenericSerialDevicesAsync(new Empty());
        return devicesResponse.DeviceNames;
    }

    protected override async Task<IEnumerable<string>> GetDeviceIds()
    {
        var devicesResponse = await _client.GetAllGenericSerialDevicesAsync(new Empty());
        return devicesResponse.DeviceNames;
    }
}
*/

using Ares.Messaging.Device;
using Google.Protobuf.WellKnownTypes;
using RestSerialDevice.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UI.Backend.ViewModels.Devices.SerialRestDevice;


public class SerialRestDeviceMultiViewModel : SerialDeviceConnectorViewModel<SerialRestDeviceUnitControlViewModel>
{
    private readonly RestSerialDeviceRpc.RestSerialDeviceRpcClient _restSerialClient;

    public SerialRestDeviceMultiViewModel(AresDevices.AresDevicesClient devicesClient, RestSerialDeviceRpc.RestSerialDeviceRpcClient restSerialClient) : base(devicesClient)
    {
        _restSerialClient = restSerialClient;
    }

    protected override SerialRestDeviceUnitControlViewModel CreateUnitVm(string deviceName)
    {
        var vm = new SerialRestDeviceUnitControlViewModel(deviceName, _restSerialClient);
        return vm;
    }

    protected override async Task<IEnumerable<string>> GetDeviceNames()
    {
        var devInfos = await _restSerialClient.GetAllGenericSerialDevicesAsync(new Empty());
        return devInfos.DeviceNames;
    }

}




