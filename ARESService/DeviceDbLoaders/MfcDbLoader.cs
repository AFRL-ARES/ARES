using AlicatMFC;
using Ares.Alicat.Mfc.Config;
using AresService.DeviceManagers;
using Microsoft.EntityFrameworkCore;

namespace AresService.DeviceDbLoaders;

public class MfcDbLoader : DeviceDbLoaderBase<IMassFlowController, MfcConfig>
{
  public MfcDbLoader(IDbContextFactory<AresDbContext> dbContextFactory, IDeviceManager<MfcConfig, IMassFlowController> deviceManager) : base(dbContextFactory, deviceManager)
  {
  }
}
