using Ares.Alicat.Mfc.Messaging;
using Ares.Messages;
using Ares.Messages.DeviceStates.Chiller;
using Ares.Messages.DeviceStates.Mfc;
using Ares.Messages.DeviceStates.RestSerialDevice;
using Ares.Messages.DeviceStates.SyringePump;
using Ares.Messages.DeviceStates.Tc0304;
using Ares.Messages.DeviceStates.TicStepperController;
using Ares.Messages.DeviceStates.TubeFurnace;
using Ares.Services;
using Ares.Services.Device;
using Ares.SyringePump.Ne1000.Messaging;
using ChemyxPumpPlugin.Services;
using Chiller.Services;
using FlirCM3.Services;
using Grpc.Health.V1;
using HerkulexDRS.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Radzen;
using RestDevice.Services;
using RestSerialDevice.Services;
using Tc0304.Services;
using TicStepperController.Messaging;
using TubeFurnace.Messaging;
using UI.Areas.Identity;
using UI.Authentication;
using UI.Backend.Devices;
using UI.Backend.Factories;
using UI.Backend.Helpers;
using UI.Backend.Notifications;
using UI.Backend.Repos;
using UI.Backend.ViewModels;
using UI.Backend.ViewModels.Automation;
using UI.Backend.ViewModels.Automation.CampaignEdit;
using UI.Backend.ViewModels.Automation.CampaignEdit.Factories;
using UI.Backend.ViewModels.Automation.Planning;
using UI.Backend.ViewModels.DeviceStateLogging;
using UI.Backend.ViewModels.Factories;
using UI.Backend.ViewModels.Misc;
using UI.Backend.ViewModels.Settings.Analysis;
using UI.Backend.ViewModels.Settings.Device.ChemyxPump;
using UI.Backend.ViewModels.Settings.Device.CM3Camera;
using UI.Backend.ViewModels.Settings.Device.LaserChiller;
using UI.Backend.ViewModels.Settings.Device.Mfc;
using UI.Backend.ViewModels.Settings.Device.Remote;
using UI.Backend.ViewModels.Settings.Device.RestDevice;
using UI.Backend.ViewModels.Settings.Device.SerialRestDevice;
using UI.Backend.ViewModels.Settings.Device.Servo;
using UI.Backend.ViewModels.Settings.Device.StepperController;
using UI.Backend.ViewModels.Settings.Device.SyringePump;
using UI.Backend.ViewModels.Settings.Device.Tc0304;
using UI.Backend.ViewModels.Settings.Device.TubeFurnace;
using UI.Backend.ViewModels.Settings.Device.ValveController;
using UI.Backend.ViewModels.Settings.Device.VerdiLaser;
using UI.Backend.ViewModels.Settings.Logging;
using UI.Backend.ViewModels.Settings.Planning;
using UI.Services.CampaignEdit;
using UI.Services.Grpc;
using UI.Services.ServerHealth;
using UI.Services.ServerHealthNotification;
using ValveController.Services;
using VerdiV6.Services;

namespace UI;

internal static class ServiceCollectionExtensions
{
  public static void LoadAresModules(this IServiceCollection services)
  {
    services.AddScoped<ServerHealthService>();
    services.AddScoped<ServerHealthNotificationService>();
    services.AddScoped<AresAuthenticationState>();
    services.AddScoped<DialogService>();
    services.AddSingleton<NotificationService>();
    services.AddScoped<TooltipService>();
    services.AddScoped<ContextMenuService>();
    services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
    services.AddSingleton<UnitCategoryHelper>();
    services.AddScoped<CampaignEditContext>();
    services.BindViewModels();
    services.BindViewModelFactories();
    services.AddScoped<ICombinedDeviceGetter, CombinedDeviceGetter>();
    services.AddSingleton<IDeviceControlViewModelRepo, DeviceControlViewModelRepo>();
    services.AddSingleton<INotificationRepository, NotificationRepository>();

    services.AddSingleton<DeviceAdapterRepository>();
    services.AddSingleton<DeviceAdapterManager>();
  }

  public static void BindClients(this IServiceCollection services)
  {
    var tempProvider = services.BuildServiceProvider();
    var clientManager = tempProvider.GetRequiredService<IClientManager>();

    //Ares Clients
    services.AddScoped(_ => clientManager.GetClient<Ares.Messages.Authentication.AuthenticationClient>());
    services.AddScoped(_ => clientManager.GetClient<AresServerInfo.AresServerInfoClient>());
    services.AddScoped(_ => clientManager.GetClient<UserManagement.UserManagementClient>());
    services.AddScoped(_ => clientManager.GetClient<AresAutomation.AresAutomationClient>());
    services.AddScoped(_ => clientManager.GetClient<Health.HealthClient>());
    services.AddScoped(_ => clientManager.GetClient<AresPlannerManagementService.AresPlannerManagementServiceClient>());
    services.AddScoped(_ => clientManager.GetClient<AresValidation.AresValidationClient>());
    services.AddScoped(_ => clientManager.GetClient<AresAnalyzerManagementService.AresAnalyzerManagementServiceClient>());
    services.AddScoped(_ => clientManager.GetClient<AresAnalysisService.AresAnalysisServiceClient>());
    services.AddScoped(_ => clientManager.GetClient<AresSafetyService.AresSafetyServiceClient>());
    services.AddSingleton(_ => clientManager.GetClient<AresNotificationRpc.AresNotificationRpcClient>());

    //Device Clients
    services.AddSingleton(_ => clientManager.GetClient<AresDevices.AresDevicesClient>());
    services.AddSingleton(_ => clientManager.GetClient<MfcRpc.MfcRpcClient>());
    services.AddSingleton(_ => clientManager.GetClient<SyringePumpRpc.SyringePumpRpcClient>());
    services.AddSingleton(_ => clientManager.GetClient<TubeFurnaceRpc.TubeFurnaceRpcClient>());
    services.AddSingleton(_ => clientManager.GetClient<TC0304Rpc.TC0304RpcClient>());
    services.AddSingleton(_ => clientManager.GetClient<HerkulexDRSRpc.HerkulexDRSRpcClient>());
    services.AddSingleton(_ => clientManager.GetClient<FlirCM3CameraRpc.FlirCM3CameraRpcClient>());
    services.AddSingleton(_ => clientManager.GetClient<ValveControllerRpc.ValveControllerRpcClient>());
    services.AddSingleton(_ => clientManager.GetClient<VerdiV6Rpc.VerdiV6RpcClient>());
    services.AddSingleton(_ => clientManager.GetClient<ChillerRpc.ChillerRpcClient>());
    services.AddSingleton(_ => clientManager.GetClient<StepperControllerRpc.StepperControllerRpcClient>());
    services.AddSingleton(_ => clientManager.GetClient<RestDeviceRpc.RestDeviceRpcClient>());
    services.AddSingleton(_ => clientManager.GetClient<RestSerialDeviceRpc.RestSerialDeviceRpcClient>());
    services.AddSingleton(_ => clientManager.GetClient<ChemyxPumpRpc.ChemyxPumpRpcClient>());

    //Device State Logging Clients
    services.AddScoped(_ => clientManager.GetClient<MfcStateLogging.MfcStateLoggingClient>());
    services.AddScoped(_ => clientManager.GetClient<Tc0304StateLogging.Tc0304StateLoggingClient>());
    services.AddScoped(_ => clientManager.GetClient<SyringePumpStateLogging.SyringePumpStateLoggingClient>());
    services.AddScoped(_ => clientManager.GetClient<TicStepperControllerStateLogging.TicStepperControllerStateLoggingClient>());
    services.AddScoped(_ => clientManager.GetClient<TubeFurnaceStateLogging.TubeFurnaceStateLoggingClient>());
    services.AddScoped(_ => clientManager.GetClient<ChillerStateLogging.ChillerStateLoggingClient>());
    services.AddScoped(_ => clientManager.GetClient<DeviceStateExportService.DeviceStateExportServiceClient>());
    services.AddScoped(_ => clientManager.GetClient<RestSerialDeviceStateLogging.RestSerialDeviceStateLoggingClient>());
  }

  private static void BindViewModels(this IServiceCollection services)
  {
    services.AddScoped<DataViewerViewModel>();
    services.AddScoped<IndexViewModel>();
    services.AddScoped<NotificationHistoryViewModel>();
    services.AddScoped<ProfileViewModel>();
    services.AddScoped<ProjectViewModel>();
    services.AddScoped<QuasiManualViewModel>();
    services.AddScoped<SettingsViewModel>();
    services.AddTransient<CampaignDesignerViewModel>();
    services.AddScoped<CampaignListViewModel>();
    services.AddScoped<CustomStepBuilderViewModel>();
    services.AddScoped<ExecutionHistoryViewModel>();
    services.AddScoped<ExecutionViewModel>();

    //Device Settings List View Models
    services.AddTransient<DeviceStatesViewModel>();
    services.AddTransient<DeviceStateExporterViewModel>();
    services.AddTransient<AnalyzerSettingsListViewModel>();
    services.AddTransient<PlannerSettingsListViewModel>();
    services.AddTransient<MfcSettingsListViewModel>();
    services.AddTransient<Tc0304SettingsListViewModel>();
    services.AddTransient<ServoSettingsListViewModel>();
    services.AddTransient<CM3CameraSettingsListViewModel>();
    services.AddTransient<ValveControllerSettingsListViewModel>();
    services.AddTransient<SyringePumpSettingsListViewModel>();
    services.AddTransient<StepperControllerSettingsListViewModel>();
    services.AddTransient<TubeFurnaceSettingsListViewModel>();
    services.AddTransient<VerdiLaserSettingsListViewModel>();
    services.AddTransient<LaserChillerSettingsListViewModel>();
    services.AddTransient<RemoteDeviceSettingsListViewModel>();
    services.AddTransient<RestDeviceSettingsListViewModel>();
    services.AddTransient<SerialRestDeviceSettingsListViewModel>();
    services.AddTransient<ChemyxPumpSettingsListViewModel>();

    //Other View Models
    services.AddTransient<DeviceStatesViewModel>();
    services.AddTransient<DeviceStateExporterViewModel>();
    services.AddScoped<ManualPlannerViewModel>();
    services.AddScoped<ManualExecutionWidgetViewModel>();
    services.AddScoped<LoggingSettingsListViewModel>();
  }
  private static void BindViewModelFactories(this IServiceCollection services)
  {
    services.AddScoped<CommandDesignerFactory>();
    services.AddScoped<CommandParameterDesignerFactory>();
    services.AddScoped<ExperimentDesignerFactory>();
    services.AddScoped<StartupDesignerFactory>();
    services.AddScoped<CloseoutDesignerFactory>();
    services.AddScoped<MetadataPickerFactory>();
    services.AddScoped<ParameterEditorFactory>();
    services.AddScoped<PlannableParameterDesignerFactory>();
    services.AddScoped<StepDesignerFactory>();
    services.AddScoped<PlanningDesignerFactory>();
    services.AddScoped<AnalyzerInputDesignerVmFactory>();
    services.AddScoped<DeviceStateFilterViewModelFactory>();
    services.AddSingleton<MFCDeviceControlViewModelFactory>();
    services.AddSingleton<SyringePumpDeviceControlViewModelFactory>();
    services.AddSingleton<Tc0304DeviceControlViewModelFactory>();
    services.AddSingleton<ServoDeviceControlViewModelFactory>();
    services.AddSingleton<ValveControllerDeviceControlViewModelFactory>();
    services.AddSingleton<TubeFurnaceDeviceControlViewModelFactory>();
    services.AddSingleton<StepperControllerDeviceControlViewModelFactory>();
    services.AddSingleton<CM3CamDeviceControlViewModelFactory>();
    services.AddSingleton<RemoteDeviceControlViewModelFactory>();
    services.AddSingleton<ChemyxPumpControlViewModelFactory>();
  }
}
