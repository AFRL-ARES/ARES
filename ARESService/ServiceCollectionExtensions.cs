using System.Collections.Generic;
using System.IO;
using AlicatMFC;
using Ares.Alicat.Mfc.Config;
using Ares.Core;
using Ares.Core.Device;
using Ares.Core.Device.State.Export;
using Ares.Core.Device.State.Export.ExportStreamProviders;
using Ares.Core.Device.State.Export.StateGetters;
using Ares.Core.Device.State.Logging;
using Ares.Core.Execution;
using Ares.Core.Grpc;
using Ares.Core.Notifications;
using Ares.SyringePump.Ne1000.Messaging;
using AresService.ConfigManagers;
using AresService.ConnectionManagement;
using AresService.DeviceDbLoaders;
using AresService.DeviceManagers;
using AresService.DeviceStateExport.ExportDataProviders;
using AresService.DeviceStateExport.StreamProviders.LaserChiller;
using AresService.DeviceStateExport.StreamProviders.Mfc;
using AresService.DeviceStateExport.StreamProviders.RestDevice;
using AresService.DeviceStateExport.StreamProviders.RestSerialDevice;
using AresService.DeviceStateExport.StreamProviders.StepperController;
using AresService.DeviceStateExport.StreamProviders.SyringePump;
using AresService.DeviceStateExport.StreamProviders.Tc0304;
using AresService.DeviceStateExport.StreamProviders.TubeFurnace;
using AresService.DeviceStateLoggers.LaserChiller;
using AresService.DeviceStateLoggers.Mfc;
using AresService.DeviceStateLoggers.SyringePump;
using AresService.DeviceStateLoggers.Tc0304;
using AresService.DeviceStateLoggers.TicStepperController;
using AresService.DeviceStateLoggers.TubeFurnace;
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
using Serilog;
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
    services.AddSingleton<ISerialConnectionRepository, SerialConnectionRepository>();

    services.AddDeviceManagers();
    services.AddDeviceStateLoggers();
    services.AddAresCoreComponents();
    services.AddNotificationHandlers();
    services.BindStateExporters();

    services.RemoveAll<IDeviceCommandInterpreterRepo>();
    services.AddSingleton<IDeviceCommandInterpreterRepo, DeviceCommandInterpreterRepo>();


    services.AddSingleton<IExecutionSummaryHandler>(provider =>
      {
        var stateExporters = provider.GetServices<IDeviceStateExportStreamProvider>();
        return new ExperimentResultJsonHandler(stateExporters);
      });

    services.AddLogging(b =>
    {
      var logPath = Path.Combine("logs", "AresServiceLog.log");
      var logger = new LoggerConfiguration()
        .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
        .CreateLogger();

      b.AddSerilog(logger);
    });
  }

  private static void AddDeviceManagers(this IServiceCollection services)
  {
    //Database Loaders
    services.AddTransient<IDeviceDbLoader, MfcDbLoader>();
    services.AddTransient<IDeviceDbLoader, Tc0304DbLoader>();
    services.AddTransient<IDeviceDbLoader, SyringePumpDbLoader>();
    services.AddTransient<IDeviceDbLoader, ServoDbLoader>();
    services.AddTransient<IDeviceDbLoader, StepperControllerDbLoader>();
    services.AddTransient<IDeviceDbLoader, TubeFurnaceDbLoader>();
    services.AddTransient<IDeviceDbLoader, ValveControllerDbLoader>();
    services.AddTransient<IDeviceDbLoader, FlirCM3CameraDbLoader>();
    services.AddTransient<IDeviceDbLoader, VerdiLaserDbLoader>();
    services.AddTransient<IDeviceDbLoader, LaserChillerDbLoader>();
    services.AddTransient<IDeviceDbLoader, RestDeviceDbLoader>();
    services.AddTransient<IDeviceDbLoader, SerialRestDeviceDbLoader>();

    //Config Managers
    services.AddTransient<IDeviceConfigManager<MfcConfig>, MfcConfigManager>();
    services.AddTransient<IDeviceConfigManager<Tc0304Config>, Tc0304ConfigManager>();
    services.AddTransient<IDeviceConfigManager<ServoConfig>, ServoConfigManager>();
    services.AddTransient<IDeviceConfigManager<SyringePumpConfig>, SyringePumpConfigManager>();
    services.AddTransient<IDeviceConfigManager<StepperControllerConfig>, StepperControllerConfigManager>();
    services.AddTransient<IDeviceConfigManager<TubeFurnaceConfig>, TubeFurnaceConfigManager>();
    services.AddTransient<IDeviceConfigManager<ValveControllerConfig>, ValveControllerConfigManager>();
    services.AddTransient<IDeviceConfigManager<FlirCM3Config>, FlirCM3ConfigManager>();
    services.AddTransient<IDeviceConfigManager<VerdiConfig>, VerdiLaserConfigManager>();
    services.AddTransient<IDeviceConfigManager<ChillerConfig>, LaserChillerConfigManager>();
    services.AddTransient<IDeviceConfigManager<RestDeviceConfig>, RestDeviceConfigManager>();
    services.AddTransient<IDeviceConfigManager<RestSerialConfig>, RestSerialDeviceConfigManager>();


    //Device Managers
    services.AddTransient<IDeviceManager<MfcConfig, IMassFlowController>, MfcManager>();
    services.AddTransient<IDeviceManager<Tc0304Config, IDataloggerThermometer>, Tc0304Manager>();
    services.AddTransient<IDeviceManager<SyringePumpConfig, ISyringePump>, SyringePumpManager>();
    services.AddTransient<IDeviceManager<ServoConfig, IServo>, ServoDeviceManager>();
    services.AddTransient<IDeviceManager<StepperControllerConfig, IStepperController>, StepperControllerManager>();
    services.AddTransient<IDeviceManager<TubeFurnaceConfig, ITubeFurnace>, TubeFurnaceManager>();
    services.AddTransient<IDeviceManager<ValveControllerConfig, IValveController>, ValveControllerDeviceManager>();
    services.AddTransient<IDeviceManager<FlirCM3Config, IFlirCM3Camera>, FlirCM3CameraDeviceManager>();
    services.AddTransient<IDeviceManager<VerdiConfig, IVerdiV6Laser>, VerdiLaserDeviceManager>();
    services.AddTransient<IDeviceManager<ChillerConfig, ILaserChiller>, LaserChillerDeviceManager>();
    services.AddTransient<IDeviceManager<RestDeviceConfig, IRestDevice>, RestDeviceManager>();
    services.AddTransient<IDeviceManager<RestSerialConfig, ISerialRestDevice>, SerialRestDeviceManager>();

    //Serial Connection Managers
    services.AddTransient<ISerialConnectionManager<IMfcConnection>, MfcSerialConnectionManager>();
    services.AddTransient<ISerialConnectionManager<IDataloggerThermometerConnection>, DataloggerSerialConnectionManager>();
    services.AddTransient<ISerialConnectionManager<IServoConnection>, ServoSerialConnectionManager>();
    services.AddTransient<ISerialConnectionManager<ISyringePumpConnection>, SyringePumpSerialConnectionManager>();
    services.AddTransient<ISerialConnectionManager<IStepperControllerConnection>, StepperControllerSerialConnectionManager>();
    services.AddTransient<ISerialConnectionManager<ITubeFurnaceConnection>, TubeFurnaceSerialConnectionManager>();
    services.AddTransient<ISerialConnectionManager<IValveControllerConnection>, ValveControllerSerialConnectionManager>();
    services.AddTransient<ISerialConnectionManager<ILaserConnection>, VerdiLaserSerialConnectionManager>();
    services.AddTransient<ISerialConnectionManager<ILaserChillerConnection>, LaserChillerSerialConnectionManager>();
    services.AddTransient<ISerialConnectionManager<ISerialRestDeviceConnection>, SerialRestDeviceConnectionManager>();
  }

  private static void BindStateExporters(this IServiceCollection services)
  {
    //Export Data Providers
    services.AddSingleton<IDeviceStateDataProvider, MfcExportDataProvider>();
    services.AddSingleton<IDeviceStateDataProvider, Tc0304ExportDataProvider>();
    services.AddSingleton<IDeviceStateDataProvider, SyringePumpExportDataProvider>();
    services.AddSingleton<IDeviceStateDataProvider, TubeFurnaceExportDataProvider>();
    services.AddSingleton<IDeviceStateDataProvider, TicStepperControllerExportDataProvider>();
    services.AddSingleton<IDeviceStateDataProvider, LaserChillerExportDataProvider>();

    //State Stream Providers
    services.AddSingleton<IDeviceStateStreamProvider, MfcStateStreamProvider>();
    services.AddSingleton<IDeviceStateStreamProvider, Tc0304StateStreamProvider>();
    services.AddSingleton<IDeviceStateStreamProvider, SyringePumpStateStreamProvider>();
    services.AddSingleton<IDeviceStateStreamProvider, TubeFurnaceStateStreamProvider>();
    services.AddSingleton<IDeviceStateStreamProvider, TicStepperControllerStateStreamProvider>();
    services.AddSingleton<IDeviceStateStreamProvider, ChillerStateStreamProvider>();
    services.AddSingleton<IDeviceStateStreamProvider, RestDeviceStateStreamProvider>();
    services.AddSingleton<IDeviceStateStreamProvider, RestSerialDeviceStateStreamProvider>();
    services.AddSingleton<IDeviceStateGetter, DeviceStateGetter>();
  }

  private static void AddDeviceStateLoggers(this IServiceCollection services)
  {
    //State Logger Factories
    services.AddSingleton<IDeviceStateLoggerFactory, MfcStateLoggerFactory>();
    services.AddSingleton<IDeviceStateLoggerFactory, Tc0304StateLoggerFactory>();
    services.AddSingleton<IDeviceStateLoggerFactory, SyringePumpStateLoggerFactory>();
    services.AddSingleton<IDeviceStateLoggerFactory, StepperControllerStateLoggerFactory>();
    services.AddSingleton<IDeviceStateLoggerFactory, TubeFurnaceStateLoggerFactory>();
    services.AddSingleton<IDeviceStateLoggerFactory, LaserChillerStateLoggerFactory>();
  }
}
