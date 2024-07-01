using AlicatMFC;
using Ares.Alicat.Mfc.Config;
using ARESCore;
using ARESCore.DeviceManagers;
using Microsoft.EntityFrameworkCore;

namespace ARESCore.DeviceDbLoaders;

public class MfcDbLoader : DeviceDbLoaderBase<IMassFlowController, MfcConfig>
{
  public MfcDbLoader(IDbContextFactory<ARESDbContext> dbContextFactory, IDeviceManager<MfcConfig, IMassFlowController> deviceManager) : base(dbContextFactory, deviceManager)
  {
  }
}
