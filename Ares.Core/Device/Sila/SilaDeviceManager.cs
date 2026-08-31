using Ares.Core.Device.Repos;
using Ares.Core.Device.State.Logging;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using Tecan.Sila2;

namespace Ares.Core.Device.Sila;

public class SilaDeviceManager : ISilaDeviceManager
{
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;
  private readonly SilaClient _silaClient;
  private readonly IAresDeviceRepo _deviceRepo;
  private readonly List<SilaDeviceMonitor> _monitors;
  private readonly ILoggerFactory _loggerFactory;
  private readonly StateLoggerManager _stateLoggerManager;

  public SilaDeviceManager(SilaClient client, 
    IAresDeviceRepo deviceRepo, 
    IDbContextFactory<CoreDatabaseContext> dbContextFactory,
    ILoggerFactory loggerFactory,
    StateLoggerManager stateLoggerManager)
  {
    _silaClient = client;
    _deviceRepo = deviceRepo;
    _dbContextFactory = dbContextFactory;
    _loggerFactory = loggerFactory;
    _monitors = new List<SilaDeviceMonitor>();
    _stateLoggerManager = stateLoggerManager;
  }

  public async Task<SilaDevice?> Create(ServerData data)
  {
    var newConfig = new SilaDeviceConfig()
    {
      UniqueId = data.Config.Uuid.ToString(),
      ServerName = data.Config.Name,
      Description = data.Info.Description,
      Type = data.Info.Type,
      VendorUri = data.Info.VendorUri,
      Version = data.Info.Version,
      Address = data.Address ?? "Unknown"
    };

    var newSilaDevice = ConfigToDevice(newConfig, data);
    _deviceRepo.AddOrUpdate(newSilaDevice);

    await newSilaDevice.Activate(CancellationToken.None);

    using var context = _dbContextFactory.CreateDbContext();
    await context.SilaConfigs.AddAsync(newConfig);
    await context.SaveChangesAsync();
    return newSilaDevice;
  }

  public async Task<SilaDevice?> Create(string address, int port)
  {
    var server = _silaClient.TryConnectToServer(address, port);

    if(server is null)
      return null;

    var newConfig = new SilaDeviceConfig()
    {
      UniqueId = server.Config.Uuid.ToString(),
      ServerName = server.Config.Name,
      Description = server.Info.Description,
      Type = server.Info.Type,
      VendorUri = server.Info.VendorUri,
      Version = server.Info.Version
    };

    var newSilaDevice = ConfigToDevice(newConfig, server);
    _deviceRepo.AddOrUpdate(newSilaDevice);
    await newSilaDevice.Activate(CancellationToken.None);
    return newSilaDevice;
  }


  public Task<IEnumerable<ServerData>> UpdateAvailableSilaDevices()
    => Task.Run(_silaClient.DiscoverServers);
  

  private SilaDevice ConfigToDevice(SilaDeviceConfig config, ServerData data)
  {
    var deviceInfo = new DeviceConnectionInfo()
    {
      DeviceId = config.UniqueId,
      DeviceName = config.ServerName,
      DeviceSettings = new AresStruct(),
      Simulated = false
    };

    var silaDevice = new SilaDevice(data, deviceInfo, _silaClient);
    return silaDevice;
  }

  public async Task LoadSilaDevices()
  {
    using var ctx = _dbContextFactory.CreateDbContext();
    var configs = await ctx.SilaConfigs.ToArrayAsync();
    var devices = await Task.WhenAll(configs.Select(LoadExistingSilaDevice));
    var nonNullDevices = devices.OfType<SilaDevice>().ToArray();
    foreach(var device in nonNullDevices)
    {
      _deviceRepo.AddOrUpdate(device);
      var monitor = new SilaDeviceMonitor(device, _loggerFactory.CreateLogger<SilaDeviceMonitor>());
      _monitors.Add(monitor);

      await _stateLoggerManager.SetupLogger(device);
    }
  }

  public async Task<SilaDevice?> LoadExistingSilaDevice(SilaDeviceConfig deviceConfig)
  {
    var parsed = IPEndPoint.TryParse(deviceConfig.Address, out var endPoint);

    if(!parsed || endPoint is null)
      return null;

    var serverData = _silaClient.TryConnectToServer(endPoint.Address.ToString(), endPoint.Port);

    if(serverData is null)
      return null;

    var device = ConfigToDevice(deviceConfig, serverData);
    await device.Activate(CancellationToken.None);
    return device;
  }
}
