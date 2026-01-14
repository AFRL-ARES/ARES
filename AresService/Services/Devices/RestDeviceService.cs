using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Device;
using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using AresService.DeviceManagers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using RestDevice;
using RestDevice.Config;
using RestDevice.Services;

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
      .GetAresDevices<IRestDevice>()
      .FirstOrDefault();

    return device;
  }

  public override async Task<AresValue> CallDeviceMethod(DeviceMethodRequest request, ServerCallContext context)
  {
    var device = GetRestDevice(request.DeviceId);

    if(device is null)
      return AresValueHelper.CreateNull();

    var response = await device.ProcessCommand(request.MethodName, request.ParameterNames.ToList(), request.ParameterValues.ToList());
    return response;
  }

  public override Task<DeviceCapabilitiesResponse> GetDeviceCapabilities(DeviceRequest request, ServerCallContext context)
  {
    var device = GetRestDevice(request.DeviceId);
    var response = new DeviceCapabilitiesResponse();

    if(device is null)
      return Task.FromResult(response);

    response.DeviceId = request.DeviceId;
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
    var device = await _deviceManager.Create(restConfig);
    await _configManager.Add(device.UniqueId, device.Name, restConfig);
    return new Empty();
  }

  public override async Task<Empty> RemoveRestDevice(DeviceRequest deviceRequest, ServerCallContext context)
  {
    await _deviceManager.Remove(deviceRequest.DeviceId);
    await _configManager.Remove(deviceRequest.DeviceId);
    return new Empty();
  }

  public override async Task<Empty> UpdateRestDevice(RestDeviceUpdateRequest request, ServerCallContext context)
  {
    await _deviceManager.Update(request.Id, request.Config);
    await _configManager.Update(request.Id, request.Config);
    return new Empty();
  }

  public override Task<GetAllRestDevicesResponse> GetAllRestDevices(Empty request, ServerCallContext context)
  {
    var devices = _deviceCommandInterpreterRepo
      .GetAresDevices<IRestDevice>()
      .Select(device => new RestDeviceDescription { Id = device.UniqueId, Name = device.Name });

    var response = new GetAllRestDevicesResponse();
    response.Devices.AddRange(devices);
    return Task.FromResult(response);
  }
}
