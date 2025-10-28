using AresService.Data;
using AresService.DeviceManagers;
using FlirCM3;
using FlirCM3.Config;
using Microsoft.EntityFrameworkCore;

namespace AresService.DeviceDbLoaders
{
  public class FlirCM3CameraDbLoader : DeviceDbLoaderBase<IFlirCM3Camera, FlirCM3Config>
  {
    public FlirCM3CameraDbLoader(IDbContextFactory<AresDbContext> dbContextFactory, IDeviceManager<FlirCM3Config, IFlirCM3Camera> deviceManager) : base(dbContextFactory, deviceManager)
    {

    }
  }
}
