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
using Radzen;
using RestDevice.Services;
using RestSerialDevice.Services;
using Tc0304.Services;
using TicStepperController.Messaging;
using TubeFurnace.Messaging;
using UI.Infrastructure.Repos;
using UI.Features.Devices.Shared;
using UI.Features.Analyzing.Settings;
using UI.Features.DeviceStateLogging.Settings;
using UI.Features.Notifications;
using UI.Features.Planning;
using UI.Features.Planning.Settings;
using UI.Features.Auth;
using UI.Features.CampaignEdit;
using UI.Features.CampaignEdit.Factories;
using UI.Features.CampaignEdit.ViewModels;
using UI.Features.Devices.ChemyxPump;
using UI.Features.Devices.CM3Camera;
using UI.Features.Devices.Mfc;
using UI.Features.Devices.Remote;
using UI.Features.Devices.Servo;
using UI.Features.Devices.StepperController;
using UI.Features.Devices.SyringePump;
using UI.Features.Devices.Tc0304;
using UI.Features.Devices.TubeFurnace;
using UI.Features.Devices.ValveController;
using UI.Features.DeviceStateExport;
using UI.Features.DeviceStateLogging;
using UI.Domain.Notifications;
using UI.Infrastructure.Monaco.Interops;
using UI.Infrastructure.Dialog;
using UI.Infrastructure.Grpc;
using UI.Infrastructure.Notifications;
using UI.Features.ServerHealth;
using ValveController.Services;
using VerdiV6.Services;
using CampaignDesignerViewModel = UI.Features.CampaignEdit.ViewModels.CampaignDesignerViewModel;
using ChemyxPumpSettingsListViewModel = UI.Features.Devices.ChemyxPump.ChemyxPumpSettingsListViewModel;
using CM3CameraSettingsListViewModel = UI.Features.Devices.CM3Camera.CM3CameraSettingsListViewModel;
using DataViewerViewModel = UI.Features.DataViewer.DataViewerViewModel;
using DeviceStatesViewModel = UI.Features.DeviceStateExport.DeviceStatesViewModel;
using ExecutionHistoryViewModel = UI.Features.ExecutionHistory.ExecutionHistoryViewModel;
using ExecutionViewModel = UI.Features.Execution.ExecutionViewModel;
using LaserChillerSettingsListViewModel = UI.Features.Devices.LaserChiller.LaserChillerSettingsListViewModel;
using ManualPlannerViewModel = UI.Features.Planning.ManualPlannerViewModel;
using MfcSettingsListViewModel = UI.Features.Devices.Mfc.MfcSettingsListViewModel;
using RemoteDeviceSettingsListViewModel = UI.Features.Devices.Remote.RemoteDeviceSettingsListViewModel;
using RestDeviceSettingsListViewModel = UI.Features.Devices.RestDevice.RestDeviceSettingsListViewModel;
using ScriptPlaygroundViewModel = UI.Features.ScriptPlayground.ScriptPlaygroundViewModel;
using SerialRestDeviceSettingsListViewModel = UI.Features.Devices.SerialRestDevice.SerialRestDeviceSettingsListViewModel;
using ServoSettingsListViewModel = UI.Features.Devices.Servo.ServoSettingsListViewModel;
using StepperControllerSettingsListViewModel = UI.Features.Devices.StepperController.StepperControllerSettingsListViewModel;
using SyringePumpSettingsListViewModel = UI.Features.Devices.SyringePump.SyringePumpSettingsListViewModel;
using Tc0304SettingsListViewModel = UI.Features.Devices.Tc0304.Tc0304SettingsListViewModel;
using TubeFurnaceSettingsListViewModel = UI.Features.Devices.TubeFurnace.TubeFurnaceSettingsListViewModel;
using ValveControllerSettingsListViewModel = UI.Features.Devices.ValveController.ValveControllerSettingsListViewModel;
using VerdiLaserSettingsListViewModel = UI.Features.Devices.VerdiV6Laser.VerdiLaserSettingsListViewModel;
using UI.Domain.Dialog;
using UI.Domain.Scripting;
using UI.Components.Formatting;

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
    services.AddScoped<IUiDialogService, RadzenUiDialogService>();
    services.AddSingleton<IUiNotificationService, RadzenUiNotificationService>();
    services.AddScoped<TooltipService>();
    services.AddScoped<ContextMenuService>();
    services.AddSingleton<UnitCategoryHelper>();
    services.AddScoped<CampaignEditContext>();
    services.BindViewModels();
    services.BindViewModelFactories();
    services.AddScoped<ICombinedDeviceGetter, CombinedDeviceGetter>();
    services.AddSingleton<IDeviceControlViewModelRepo, DeviceControlViewModelRepo>();
    services.AddSingleton<INotificationRepository, NotificationRepository>();

    services.AddSingleton<DeviceAdapterRepository>();
    services.AddSingleton<DeviceAdapterManager>();
    services.AddSingleton<MonacoCompletionProvider>();
    services.AddSingleton<MonacoDiagnosticsProvider>();
    services.AddSingleton<MonacoSemanticTokensProvider>();
    services.AddSingleton<MonacoHoverProvider>();
    services.AddSingleton<IMonacoCompletionProvider>(provider => provider.GetRequiredService<MonacoCompletionProvider>());
    services.AddSingleton<IMonacoDiagnosticsProvider>(provider => provider.GetRequiredService<MonacoDiagnosticsProvider>());
    services.AddSingleton<IMonacoSemanticTokensProvider>(provider => provider.GetRequiredService<MonacoSemanticTokensProvider>());
    services.AddSingleton<IMonacoHoverProvider>(provider => provider.GetRequiredService<MonacoHoverProvider>());
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
    services.AddSingleton(_ => clientManager.GetClient<AresScriptingService.AresScriptingServiceClient>());

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
    services.AddScoped<NotificationHistoryViewModel>();
    services.AddScoped<ProfileViewModel>();
    services.AddTransient<CampaignDesignerViewModel>();
    services.AddScoped<CampaignListViewModel>();
    services.AddScoped<ExecutionHistoryViewModel>();
    services.AddScoped<ExecutionViewModel>();
    services.AddScoped<ScriptPlaygroundViewModel>();

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
    services.AddScoped<ManualDeviceLoggerWidgetViewModel>();
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
