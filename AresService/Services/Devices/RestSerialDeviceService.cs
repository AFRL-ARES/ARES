using System.Linq;
using System.Threading.Tasks;
using Ares.Core.Device;
using Ares.Core.Device.Repos;
using AresService.DeviceManagers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using RestSerialDevice;
using RestSerialDevice.Config;
using RestSerialDevice.Services;

namespace AresService.Services.Devices;

public class RestSerialDeviceService : RestSerialDeviceRpc.RestSerialDeviceRpcBase
{
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  private readonly IDeviceManager<RestSerialConfig, ISerialRestDevice> _deviceManager;
  private readonly IDeviceConfigManager<RestSerialConfig> _configManager;

  public RestSerialDeviceService(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo,
      IDeviceManager<RestSerialConfig, ISerialRestDevice> deviceManager,
      IDeviceConfigManager<RestSerialConfig> configManager)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _deviceManager = deviceManager;
    _configManager = configManager;
  }

  private ISerialRestDevice? GetRestSerialDevice(string id)
  {
    var device = _deviceCommandInterpreterRepo
        .GetAresDevices<ISerialRestDevice>()
        .FirstOrDefault(d => d.UniqueId == id); // <--- Add filtering by name
    return device;
  }

  public override async Task<DeviceMethodResponse> CallDeviceMethod(DeviceMethodRequest request,
      ServerCallContext context)
  {
    var device = GetRestSerialDevice(request.DeviceId);

    if(device is null)
      return new DeviceMethodResponse();
    var response = await device.ProcessCommand(request.MethodName, request.ParameterNames.ToList(),
        request.ParameterValues.ToList());
    return response;
  }

  public override Task<DeviceCapabilitiesResponse> GetDeviceCapabilities(DeviceRequest request,
      ServerCallContext context)
  {
    var device = GetRestSerialDevice(request.DeviceId);
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

  public override async Task<Empty> AddGenericSerialDevice(RestSerialConfig config, ServerCallContext context)
  {
    var device = await _deviceManager.Create(config);
    await _configManager.Add(device.UniqueId, device.Name, config);
    return new Empty();
  }

  public override async Task<Empty> RemoveGenericSerialDevice(DeviceRequest deviceRequest,
      ServerCallContext context)
  {
    await _deviceManager.Remove(deviceRequest.DeviceId);
    await _configManager.Remove(deviceRequest.DeviceId);
    return new Empty();
  }

  public override async Task<Empty> UpdateGenericSerialDevice(GenericSerialRestDeviceUpdateRequest request, ServerCallContext context)
  {
    await _deviceManager.Update(request.Id, request.Config);
    await _configManager.Update(request.Id, request.Config);
    return new Empty();
  }

  public override Task<GetAllGenericSerialDevicesResponse> GetAllGenericSerialDevices(Empty request,
      ServerCallContext context)
  {
    var devices = _deviceCommandInterpreterRepo
        .GetAresDevices<ISerialRestDevice>()
        .Select(device => new DeviceDescription { Id = device.UniqueId, Name = device.Name });

    var response = new GetAllGenericSerialDevicesResponse();
    response.Devices.AddRange(devices);
    return Task.FromResult(response);
  }

  public override async Task<DataResponse> GetData(DeviceRequest request, ServerCallContext context)
  {
    var device = GetRestSerialDevice(request.DeviceId);
    var response = new DataResponse(); // This is the top-level Protobuf message

    if(device is null)
    {
      // For a gRPC service, it's often better to throw an RpcException for clear error states
      throw new RpcException(new Status(StatusCode.NotFound, $"Device '{request.DeviceId}' not found."));
      // return response; // Or return a default empty response
    }

    var readDataResponse = await device.GetAndUpdateState(); // Gets data from your SerialRestDevice

    if(readDataResponse is null)
    {
      // Handle case where no data was read, e.g., device offline, timeout
      throw new RpcException(new Status(StatusCode.Unavailable, $"Failed to get data from device '{request.DeviceId}'."));
      // return response;
    }

    // Initialize the nested 'data' message
    response.Data = new RestSerialDevice.DataModel.Data(); // 'Data' is the C# class generated from 'rest_serial_device.data_model.Data'

    // Populate the nested 'data' message's fields
    response.Data.DeviceId = request.DeviceId; // Set the device name in the nested data

    // Copy the key-value pairs from ReadDataResponse.Values to response.Data.Values
    if(readDataResponse.Values != null)
    {
      foreach(var kvp in readDataResponse.Values)
      {
        response.Data.Values.Add(kvp.Key, kvp.Value); // Uses the Protobuf MapField's Add method
      }
    }

    return response;
  }
}