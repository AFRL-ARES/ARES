using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Ares.Core.Device;
using AresService.DeviceManagers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Tc0304.Config;
using Tc0304.DataModel;
using Tc0304.Services;
using TC0304;
using TC0304.Extensions;

namespace AresService.Services.Devices;

public class Tc0304Service : TC0304Rpc.TC0304RpcBase
{
  private readonly IDeviceConfigManager<Tc0304Config> _configManager;
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  private readonly IDeviceManager<Tc0304Config, IDataloggerThermometer> _deviceManager;

  public Tc0304Service(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo, IDeviceConfigManager<Tc0304Config> configManager, IDeviceManager<Tc0304Config, IDataloggerThermometer> tc0304DeviceManager)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
    _configManager = configManager;
    _deviceManager = tc0304DeviceManager;
  }

  private IDataloggerThermometer? GetDataLogger(string id)
  {
    var dataLogger = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<IDataloggerThermometer>()
      .FirstOrDefault(device => device.UniqueId == id);

    return dataLogger;
  }

  public override Task<Empty> Hold(DeviceRequest request, ServerCallContext context)
  {
    var dataLogger = GetDataLogger(request.DeviceId);

    if(dataLogger is not null)
      dataLogger.Hold();

    return Task.FromResult(new Empty());
  }

  public override Task<DataResponse> GetData(DeviceRequest request, ServerCallContext context)
  {
    var response = new DataResponse();
    var dataLogger = GetDataLogger(request.DeviceId);
    if(dataLogger is not null)
    {
      var data = dataLogger.StateStream.Take(1).Wait();
      response.Data = data?.ToProto();
      return Task.FromResult(response);
    }

    return Task.FromResult(response);
  }

  public override Task<Empty> StartStateUpdater(StartStateUpdaterRequest request, ServerCallContext context)
  {
    var dataLogger = GetDataLogger(request.DeviceRequest.DeviceId);

    if(dataLogger is not null)
      dataLogger.StartStateUpdater(request.Interval?.ToTimeSpan() ?? TimeSpan.FromMilliseconds(250));

    return Task.FromResult(new Empty());
  }

  public override Task<Empty> StopStateUpdater(DeviceRequest request, ServerCallContext context)
  {
    var dataLogger = GetDataLogger(request.DeviceId);

    if(dataLogger is not null)
      dataLogger.StopStateUpdater();

    return Task.FromResult(new Empty());
  }

  public override async Task<Empty> AddTc0304(Tc0304Config request, ServerCallContext context)
  {
    var device = await _deviceManager.Create(request);
    await _configManager.Add(device.UniqueId, device.Name, request);
    return new Empty();
  }

  public override async Task<Empty> RemoveTc0304(Tc0304Request request, ServerCallContext context)
  {
    await _deviceManager.Remove(request.Tc0304Id);
    await _configManager.Remove(request.Tc0304Id);
    return new Empty();
  }

  public override async Task<Empty> UpdateTc0304(UpdateTc0304Request request, ServerCallContext context)
  {
    await _deviceManager.Update(request.Id, request.Config);
    await _configManager.Update(request.Id, request.Config);
    return new Empty();
  }

  public override async Task<ProbeNames> GetProbeNames(Tc0304Request request, ServerCallContext context)
    => await base.GetProbeNames(request, context);

  public override Task<GetAllTc0304sResponse> GetAllTc0304s(Empty request, ServerCallContext context)
  {
    var dataLoggers = _deviceCommandInterpreterRepo
      .Select(interpreter => interpreter.Device)
      .OfType<IDataloggerThermometer>()
      .Select(thermometer => new DeviceDescription { Id = thermometer.UniqueId, Name = thermometer.Name });

    var response = new GetAllTc0304sResponse();
    response.Devices.AddRange(dataLoggers);

    return Task.FromResult(response);
  }
}
