using Ares.Core.Device.Repos;
using Ares.Core.Execution;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore;
using Tecan.Sila2;
using Tecan.Sila2.DynamicClient;

namespace Ares.Core.Device.Sila;

public class SilaDeviceManager : ISilaDeviceManager
{
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;
  private readonly SilaClient _silaClient;
  private readonly IAresDeviceRepo _deviceRepo;

  public SilaDeviceManager(SilaClient client, 
    IAresDeviceRepo deviceRepo, 
    IDbContextFactory<CoreDatabaseContext> dbContextFactory)
  {
    _silaClient = client;
    _deviceRepo = deviceRepo;
    _dbContextFactory = dbContextFactory;
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
      Version = data.Info.Version
    };

    var newSilaDevice = ConfigToDevice(newConfig, data);
    _deviceRepo.AddOrUpdate(newSilaDevice);

    await newSilaDevice.Activate(CancellationToken.None);
    //Commenting out for testing
    //using var context = _dbContextFactory.CreateDbContext();
    //await context.SilaConfigs.AddAsync(newConfig);
    //await context.SaveChangesAsync();
    return newSilaDevice;
  }

  public async Task UpdateAvailableSilaDevices()
  {
    var available_servers = _silaClient.DiscoverServers();
    var existingSilaDevices = _deviceRepo.GetAll<SilaDevice>();

    var unregistered_servers = available_servers.Where(s => !existingSilaDevices.Any(d => d.Name == s.Config.Name));
    var missing_servers = existingSilaDevices.Where(d => !available_servers.Any(s => s.Config.Name == d.Name));

    foreach(var server in unregistered_servers)
    {
      //Already exists, no action needed
      if(existingSilaDevices.Any(d => d.UniqueId == server.Config.Uuid.ToString()))
        continue;

      await Create(server);
    }
  }

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
}
