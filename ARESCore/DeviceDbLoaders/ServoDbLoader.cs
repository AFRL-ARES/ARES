using ARESCore;
using ARESCore.DeviceManagers;
using HerkulexDRS;
using HerkulexDRS.Config;
using Microsoft.EntityFrameworkCore;

namespace ARESCore.DeviceDbLoaders;
public class ServoDbLoader : DeviceDbLoaderBase<IServo, ServoConfig>
{
  public ServoDbLoader(IDbContextFactory<ARESDbContext> dbContextFactory, IDeviceManager<ServoConfig, IServo> deviceManager) : base(dbContextFactory, deviceManager)
  {
  }
}
