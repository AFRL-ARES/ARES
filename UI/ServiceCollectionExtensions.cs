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
using Grpc.Health.V1;
using Radzen;
using UI.Features.Analyzing.Settings;
using UI.Features.DeviceStateLogging.Settings;
using UI.Features.Notifications;
using UI.Features.Planning.Settings;
using UI.Features.Auth;
using UI.Features.CampaignEdit;
using UI.Features.CampaignEdit.Factories;
using UI.Features.CampaignEdit.ViewModels;
using UI.Features.Devices.Remote;
using UI.Features.DeviceStateExport;
using UI.Features.DeviceStateLogging;
using UI.Application.Notifications;
using UI.Infrastructure.Monaco.Interops;
using UI.Infrastructure.Dialog;
using UI.Infrastructure.Grpc;
using UI.Infrastructure.Notifications;
using UI.Features.ServerHealth;
using CampaignDesignerViewModel = UI.Features.CampaignEdit.ViewModels.CampaignDesignerViewModel;
using DataViewerViewModel = UI.Features.DataViewer.DataViewerViewModel;
using DeviceStatesViewModel = UI.Features.DeviceStateExport.DeviceStatesViewModel;
using ExecutionHistoryViewModel = UI.Features.ExecutionHistory.ExecutionHistoryViewModel;
using ExecutionViewModel = UI.Features.Execution.ExecutionViewModel;
using ManualPlannerViewModel = UI.Features.Execution.Planning.ManualPlannerViewModel;
using RemoteDeviceSettingsListViewModel = UI.Features.Devices.Remote.RemoteDeviceSettingsListViewModel;
using ScriptPlaygroundViewModel = UI.Features.ScriptPlayground.ScriptPlaygroundViewModel;
using UI.Application.Dialog;
using UI.Application.Scripting;
using UI.Components.Formatting;
using UI.Infrastructure.DeviceStateLogging;
using UI.Application.DeviceStateLogging;
using UI.Infrastructure.Auth;
using UI.Infrastructure.Devices;
using UI.Application.Devices.Repos;

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

    services.AddSingleton<IDeviceDriverRepository, DeviceDriverRepository>();
    services.AddSingleton<IDeviceAdapterRepository, DeviceAdapterRepository>();
    services.AddSingleton<DeviceAdapterManager>();
    services.AddSingleton<DeviceDriverSyncManager>(sp => 
    {
        var client = sp.GetRequiredService<AresDeviceDriverService.AresDeviceDriverServiceClient>();
        var repo = sp.GetRequiredService<IDeviceDriverRepository>();
        var localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        return new DeviceDriverSyncManager(client, repo, localPath);
    });
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
    services.AddScoped(_ => clientManager.GetClient<Authentication.AuthenticationClient>());
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
    services.AddSingleton(_ => clientManager.GetClient<AresDeviceDriverService.AresDeviceDriverServiceClient>());

    //Device Clients
    services.AddSingleton(_ => clientManager.GetClient<AresDevices.AresDevicesClient>());

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
    services.AddTransient<RemoteDeviceSettingsListViewModel>();

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
    services.AddSingleton<RemoteDeviceControlViewModelFactory>();
  }
}

