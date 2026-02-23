using AlicatMFC;
using Ares.Alicat.Mfc.Config;
using Ares.Core;
using Ares.Core.Device.State.Export;
using Ares.Core.Device.State.Export.ExportStreamProviders;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Core.Device.State.Logging;
using Ares.Core.Execution;
using Ares.Core.Grpc;
using Ares.SyringePump.Ne1000.Messaging;
using AresService.DeviceDbLoaders;
using AresService.DeviceManagers;
using ChemyxPumpPlugin;
using ChemyxPumpPlugin.Config;
using Chiller.Config;
using FlirCM3;
using FlirCM3.Config;
using HerkulexDRS;
using HerkulexDRS.Config;
using LaserChiller;
using LindbergFurnace;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RestDevice;
using RestDevice.Config;
using RestSerialDevice;
using RestSerialDevice.Config;
using SyringePumpNE1000;
using Tc0304.Config;
using TC0304;
using TicStepperController;
using TicStepperController.Config;
using TubeFurnace.Config;
using ValveController;
using ValveController.Config;
using VerdiV6.Config;
using VerdiV6Laser;

namespace AresService;

public static class ServiceCollectionExtensions
{
  public static void AddAres(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddSingleton<AresStarter>();
    services.AddAresCoreComponents();
    services.AddNotificationHandlers();

    services.AddSingleton<IExecutionSummaryHandler>(provider =>
      {
        var stateExporters = provider.GetServices<IDeviceStateExportStreamProvider>();
        return new ExperimentResultJsonHandler(stateExporters);
      });
  }
}
