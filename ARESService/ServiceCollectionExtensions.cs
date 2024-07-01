
using AlicatMFC;
using Ares.Alicat.Mfc.Config;
using Ares.Core;
using Ares.Core.Device;
using Ares.SyringePump.Ne1000.Messaging;
using ARESCore.ConfigManagers;
using ARESCore.ConnectionManagement;
using ARESCore.DeviceDbLoaders;
using ARESCore.DeviceManagers;
using ARESCore.DeviceStateLoggers;
using ARESCore.DeviceStateLoggers.Mfc;
using ARESCore.DeviceStateLoggers.SyringePump;
using ARESCore.DeviceStateLoggers.Tc0304;
using ARESCore.DeviceStateLoggers.TicStepperController;
using ARESCore.DeviceStateLoggers.TubeFurnace;
using HerkulexDRS;
using HerkulexDRS.Config;
using LindbergFurnace;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SyringePumpNE1000;
using Tc0304.Config;
using TC0304;
using TicStepperController;
using TicStepperController.Config;
using TubeFurnace.Config;
using ValveController;
using ValveController.Config;

#pragma warning disable CS8603
#pragma warning disable CS8602

namespace ARESService;

public static class ServiceCollectionExtensions
{
  public static void AddARES(this IServiceCollection services)
  {
    services.AddSingleton<ARESStarter>();
    services.AddSingleton<IConnectionRepository, ConnectionRepository>();

    services.AddDeviceManagers();
    services.AddDeviceStateLoggers();
    services.AddAresCoreComponents();

    services.RemoveAll<IDeviceCommandInterpreterRepo>();
    services.AddSingleton<IDeviceCommandInterpreterRepo, DeviceCommandInterpreterRepo>();
  }

  private static void AddDeviceManagers(this IServiceCollection services)
  {
    services.AddTransient<IDeviceDbLoader, MfcDbLoader>();
    services.AddTransient<IDeviceDbLoader, Tc0304DbLoader>();
    services.AddTransient<IDeviceDbLoader, SyringePumpDbLoader>();
    services.AddTransient<IDeviceDbLoader, ServoDbLoader>();
    services.AddTransient<IDeviceDbLoader, StepperControllerDbLoader>();
    services.AddTransient<IDeviceDbLoader, TubeFurnaceDbLoader>();
    services.AddTransient<IDeviceDbLoader, ValveControllerDbLoader>();

    services.AddTransient<IDeviceConfigManager<MfcConfig>, MfcConfigManager>();
    services.AddTransient<IDeviceConfigManager<Tc0304Config>, Tc0304ConfigManager>();
    services.AddTransient<IDeviceConfigManager<ServoConfig>, ServoConfigManager>();
    services.AddTransient<IDeviceConfigManager<SyringePumpConfig>, SyringePumpConfigManager>();
    services.AddTransient<IDeviceConfigManager<StepperControllerConfig>, StepperControllerConfigManager>();
    services.AddTransient<IDeviceConfigManager<TubeFurnaceConfig>, TubeFurnaceConfigManager>();
    services.AddTransient<IDeviceConfigManager<ValveControllerConfig>, ValveControllerConfigManager>();

    services.AddTransient<IDeviceManager<MfcConfig, IMassFlowController>, MfcManager>();
    services.AddTransient<IDeviceManager<Tc0304Config, IDataloggerThermometer>, Tc0304Manager>();
    services.AddTransient<IDeviceManager<SyringePumpConfig, ISyringePump>, SyringePumpManager>();
    services.AddTransient<IDeviceManager<ServoConfig, IServo>, ServoDeviceManager>();
    services.AddTransient<IDeviceManager<StepperControllerConfig, IStepperController>, StepperControllerManager>();
    services.AddTransient<IDeviceManager<TubeFurnaceConfig, ITubeFurnace>, TubeFurnaceManager>();
    services.AddTransient<IDeviceManager<ValveControllerConfig, IValveController>, ValveControllerDeviceManager>();

    services.AddTransient<IConnectionManager<IMfcConnection>, MfcConnectionManager>();
    services.AddTransient<IConnectionManager<IDataloggerThermometerConnection>, DataloggerConnectionManager>();
    services.AddTransient<IConnectionManager<IServoConnection>, ServoConnectionManager>();
    services.AddTransient<IConnectionManager<ISyringePumpConnection>, SyringePumpConnectionManager>();
    services.AddTransient<IConnectionManager<IStepperControllerConnection>, StepperControllerConnectionManager>();
    services.AddTransient<IConnectionManager<ITubeFurnaceConnection>, TubeFurnaceConnectionManager>();
    services.AddTransient<IConnectionManager<IValveControllerConnection>, ValveControllerConnectionManager>();
  }

  private static void AddDeviceStateLoggers(this IServiceCollection services)
  {
    services.AddSingleton<IDeviceStateLoggerFactory<IMassFlowController, IMfcStateLogger>, MfcStateLoggerFactory>();
    services.AddSingleton<IDeviceStateLoggerFactory<IDataloggerThermometer, ITc0304StateLogger>, Tc0304StateLoggerFactory>();
    services.AddSingleton<IDeviceStateLoggerFactory<ISyringePump, ISyringePumpStateLogger>, SyringePumpStateLoggerFactory>();
    services.AddSingleton<IDeviceStateLoggerFactory<IStepperController, IStepperControllerStateLogger>, StepperControllerStateLoggerFactory>();
    services.AddSingleton<IDeviceStateLoggerFactory<ITubeFurnace, ITubeFurnaceStateLogger>, TubeFurnaceStateLoggerFactory>();

    services.AddSingleton<IDeviceStateLoggerRepository, DeviceStateLoggerRepository>();
  }
}
