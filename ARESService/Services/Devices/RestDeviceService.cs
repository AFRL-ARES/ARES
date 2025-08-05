using Ares.Core.Device;
using Ares.Messaging;
using Ares.Tools;
using AresService.DeviceManagers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using RestDevice;
using RestDevice.Config;
using RestDevice.Services;
using System.Linq;
using System.Threading.Tasks;

namespace AresService.Services.Devices;

public class RestDeviceService : RestDeviceRpc.RestDeviceRpcBase
{
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  private readonly IDeviceManager<RestDeviceConfig, IRestDevice> _deviceManager;
  private readonly IDeviceConfigManager<RestDeviceConfig> _configManager;

  public RestDeviceService(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
    IDeviceManager<RestDeviceConfig, IRestDevice> deviceManager,
    IDeviceConfigManager<RestDeviceConfig> configManager)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _deviceManager = deviceManager;
    _configManager = configManager;
  }

  private IRestDevice? GetRestDevice(string name)
  {
    var device = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<IRestDevice>()
      .FirstOrDefault();

    return device;
  }

  public override async Task<AresValue> CallDeviceMethod(DeviceMethodRequest request, ServerCallContext context)
  {
    var device = GetRestDevice(request.DeviceName);

    if(device is null)
      return AresValueHelper.CreateNull();

    var response = await device.ProcessCommand(request.MethodName, request.ParameterNames.ToList(), request.ParameterValues.ToList());
    return response;
  }

  public override Task<DeviceCapabilitiesResponse> GetDeviceCapabilities(DeviceRequest request, ServerCallContext context)
  {
    var device = GetRestDevice(request.DeviceName);
    var response = new DeviceCapabilitiesResponse();

    if(device is null)
      return Task.FromResult(response);

    response.DeviceName = request.DeviceName;
    foreach(var func in device.Functions)
    {
      var info = new DeviceMethodInfo();
      info.MethodName = func.Name;
      info.Parameters.AddRange(func.Parameters.Select(p => p.Name));
      response.DeviceMethods.Add(info);
    }

    return Task.FromResult(response);
  }

  public override Task<DataResponse> GetData(DeviceRequest request, ServerCallContext context)
  {
    return base.GetData(request, context);
  }

  public override async Task<Empty> AddRestDevice(RestDeviceConfig restConfig, ServerCallContext context)
  {
    await _deviceManager.Load(restConfig);
    var device = GetRestDevice(restConfig.Name);
    await _configManager.Add(restConfig.Name, restConfig);
    return new Empty();
  }

  public override async Task<Empty> RemoveRestDevice(DeviceRequest deviceRequest, ServerCallContext context)
  {
    await _deviceManager.Remove(deviceRequest.DeviceName);
    await _configManager.Remove(deviceRequest.DeviceName);
    return new Empty();
  }

  public override async Task<Empty> UpdateRestDevice(RestDeviceConfig config, ServerCallContext context)
  {
    await _deviceManager.Update(config);
    await _configManager.Update(config.Name, config);
    return new Empty();
  }

  public override Task<GetAllRestDevicesResponse> GetAllRestDevices(Empty request, ServerCallContext context)
  {
    var deviceNames = _deviceCommandInterpreterRepo
      .Select(deviceInterpeter => deviceInterpeter.Device)
      .OfType<IRestDevice>()
      .Select(device => device.Name);

    var response = new GetAllRestDevicesResponse();
    response.DeviceNames.AddRange(deviceNames);
    return Task.FromResult(response);
  }
}
