using Ares.Alicat.Mfc.Messaging;
using Ares.Messages;
using Ares.Messages.DeviceStates.Mfc;
using Ares.Messages.DeviceStates.SyringePump;
using Ares.Messages.DeviceStates.Tc0304;
using Ares.Messages.DeviceStates.TicStepperController;
using Ares.Messages.DeviceStates.TubeFurnace;
using Ares.Messaging;
using Ares.Messaging.Device;
using Ares.SyringePump.Ne1000.Messaging;
using Grpc.Health.V1;
using HerkulexDRS.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Radzen;
using Tc0304.Services;
using TicStepperController.Messaging;
using TubeFurnace.Messaging;
using UI.Areas.Identity;
using UI.Authentication;
using UI.Backend.DeviceStateExport.ExportDataProviders;
using UI.Backend.DeviceStateExport.ExportDataProviders.Devices;
using UI.Backend.DeviceStateExport.ExportStreamProviders;
using UI.Backend.DeviceStateExport.StateGetters;
using UI.Backend.DeviceStateExport.StreamProviders;
using UI.Backend.DeviceStateExport.StreamProviders.Mfc;
using UI.Backend.DeviceStateExport.StreamProviders.StepperController;
using UI.Backend.DeviceStateExport.StreamProviders.SyringePump;
using UI.Backend.DeviceStateExport.StreamProviders.Tc0304;
using UI.Backend.DeviceStateExport.StreamProviders.TubeFurnace;
using UI.Backend.Helpers;
using UI.Backend.ViewModels;
using UI.Backend.ViewModels.Automation;
using UI.Backend.ViewModels.Automation.CampaignEdit;
using UI.Backend.ViewModels.Devices.HerkulexDRS;
using UI.Backend.ViewModels.Devices.Mfc;
using UI.Backend.ViewModels.Devices.ValveController;
using UI.Backend.ViewModels.DeviceStateLogging;
using UI.Backend.ViewModels.Factories;
using UI.Backend.ViewModels.Misc;
using UI.Backend.ViewModels.Settings.Device.Mfc;
using UI.Backend.ViewModels.Settings.Device.Servo;
using UI.Backend.ViewModels.Settings.Device.StepperController;
using UI.Backend.ViewModels.Settings.Device.SyringePump;
using UI.Backend.ViewModels.Settings.Device.Tc0304;
using UI.Backend.ViewModels.Settings.Device.TubeFurnace;
using UI.Backend.ViewModels.Settings.Device.ValveController;
using UI.Backend.ViewModels.StepperController;
using UI.Backend.ViewModels.SyringePump;
using UI.Backend.ViewModels.Tc0304;
using UI.Backend.ViewModels.TubeFurnace;
using UI.Services.CampaignEdit;
using UI.Services.Grpc;
using UI.Services.ServerHealth;
using UI.Services.ServerHealthNotification;
using ValveController.Services;

namespace UI;

internal static class ServiceCollectionExtensions
{
  public static void LoadARESModules(this IServiceCollection services)
  {
    services.AddScoped<ServiceStarter>();
    services.AddScoped<ServerHealthService>();
    services.AddScoped<ServerHealthNotificationService>();
    services.AddScoped<AresAuthenticationState>();
    services.AddScoped<DialogService>();
    services.AddScoped<NotificationService>();
    services.AddScoped<TooltipService>();
    services.AddScoped<ContextMenuService>();
    services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
    services.AddSingleton<UnitCategoryHelper>();
    services.AddScoped<CampaignEditContext>();
    services.BindViewModels();
    services.BindViewModelFactories();
    services.BindStateExporters();
  }

  public static void BindClients(this IServiceCollection services)
  {
    var tempProvider = services.BuildServiceProvider();
    var clientManager = tempProvider.GetRequiredService<IClientManager>();
    services.AddScoped(_ => clientManager.GetClient<Ares.Messages.Authentication.AuthenticationClient>());
    services.AddScoped(_ => clientManager.GetClient<AresDevices.AresDevicesClient>());
    services.AddScoped(_ => clientManager.GetClient<MfcRpc.MfcRpcClient>());
    services.AddScoped(_ => clientManager.GetClient<SyringePumpRpc.SyringePumpRpcClient>());
    services.AddScoped(_ => clientManager.GetClient<TubeFurnaceRpc.TubeFurnaceRpcClient>());
    services.AddScoped(_ => clientManager.GetClient<AresServerInfo.AresServerInfoClient>());
    services.AddScoped(_ => clientManager.GetClient<UserManagement.UserManagementClient>());
    services.AddScoped(_ => clientManager.GetClient<AresAutomation.AresAutomationClient>());
    services.AddScoped(_ => clientManager.GetClient<Health.HealthClient>());
    services.AddScoped(_ => clientManager.GetClient<AresPlanning.AresPlanningClient>());
    services.AddScoped(_ => clientManager.GetClient<AresValidation.AresValidationClient>());
    services.AddScoped(_ => clientManager.GetClient<TC0304Rpc.TC0304RpcClient>());
    services.AddScoped(_ => clientManager.GetClient<HerkulexDRSRpc.HerkulexDRSRpcClient>());
    services.AddScoped(_ => clientManager.GetClient<ValveControllerRpc.ValveControllerRpcClient>());
    services.AddScoped(_ => clientManager.GetClient<MfcStateLogging.MfcStateLoggingClient>());
    services.AddScoped(_ => clientManager.GetClient<Tc0304StateLogging.Tc0304StateLoggingClient>());
    services.AddScoped(_ => clientManager.GetClient<SyringePumpStateLogging.SyringePumpStateLoggingClient>());
    services.AddScoped(_ => clientManager.GetClient<StepperControllerRpc.StepperControllerRpcClient>());
    services.AddScoped(_ => clientManager.GetClient<TicStepperControllerStateLogging.TicStepperControllerStateLoggingClient>());
    services.AddScoped(_ => clientManager.GetClient<TubeFurnaceStateLogging.TubeFurnaceStateLoggingClient>());
  }

  private static void BindViewModels(this IServiceCollection services)
  {
    services.AddScoped<DataViewerViewModel>();
    services.AddScoped<IndexViewModel>();
    services.AddScoped<NotificationsViewModel>();
    services.AddScoped<ProfileViewModel>();
    services.AddScoped<ProjectViewModel>();
    services.AddScoped<QuasiManualViewModel>();
    services.AddScoped<SettingsViewModel>();
    services.AddTransient<CampaignDesignerViewModel>();
    services.AddScoped<CampaignListViewModel>();
    services.AddScoped<CustomStepBuilderViewModel>();
    services.AddScoped<ExecutionHistoryViewModel>();
    services.AddScoped<ExecutionViewModel>();
    services.AddTransient<MfcSettingsListViewModel>();
    services.AddTransient<Tc0304SettingsListViewModel>();
    services.AddTransient<ServoSettingsListViewModel>();
    services.AddTransient<ValveControllerSettingsListViewModel>();
    services.AddTransient<SyringePumpSettingsListViewModel>();
    services.AddTransient<StepperControllerSettingsListViewModel>();
    services.AddTransient<TubeFurnaceSettingsListViewModel>();
    services.AddTransient<DeviceStatesViewModel>();
    services.AddTransient<DeviceStateExporterViewModel>();
    // services.AddTransient<StepDesignerViewModel>();
    // services.AddTransient<PlannableParameterDesignerViewModel>();
    // services.AddTransient<ParameterEditorViewModel>();
    // services.AddTransient<MetadataPickerViewModel>();
    // services.AddScoped<PlanningViewModel>();
    services.AddScoped<MfcDirectorControlViewModel>();
    services.AddScoped<SyringePumpWorkspaceControlViewModel>();
    services.AddScoped<Tc0304MultiViewModel>();
    services.AddScoped<ServoMultiViewModel>();
    services.AddScoped<ValveControllerMultiViewModel>();
    services.AddScoped<TubeFurnaceMultiViewModel>();
    services.AddScoped<StepperControllerMultiViewModel>();
    services.AddScoped<ManualExecutionWidgetViewModel>();
  }

  private static void BindStateExporters(this IServiceCollection services)
  {
    services.AddScoped<IDeviceStateExportStreamProvider, CombinedDeviceStateExportStreamProvider>();
    services.AddScoped<IDeviceStateExportStreamProvider, ZippedStatesExportStreamProvider>();

    services.AddScoped<IDeviceStateDataProvider, MfcExportDataProvider>();
    services.AddScoped<IDeviceStateDataProvider, Tc0304ExportDataProvider>();
    services.AddScoped<IDeviceStateDataProvider, SyringePumpExportDataProvider>();
    services.AddScoped<IDeviceStateDataProvider, TubeFurnaceExportDataProvider>();
    services.AddScoped<IDeviceStateDataProvider, TicStepperControllerExportDataProvider>();

    services.AddScoped<IDeviceStateStreamProvider, MfcStateStreamProvider>();
    services.AddScoped<IDeviceStateStreamProvider, Tc0304StateStreamProvider>();
    services.AddScoped<IDeviceStateStreamProvider, SyringePumpStateStreamProvider>();
    services.AddScoped<IDeviceStateStreamProvider, TubeFurnaceStateStreamProvider>();
    services.AddScoped<IDeviceStateStreamProvider, TicStepperControllerStateStreamProvider>();

    services.AddScoped<IDeviceStateGetter<MfcState>, MfcStateGetter>();
    services.AddScoped<IDeviceStateGetter<Tc0304State>, Tc0304StateGetter>();
    services.AddScoped<IDeviceStateGetter<SyringePumpState>, SyringePumpStateGetter>();
    services.AddScoped<IDeviceStateGetter<TicStepperControllerState>, StepperControllerStateGetter>();
    services.AddScoped<IDeviceStateGetter<TubeFurnaceStateEntity>, TubeFurnaceStateGetter>();

    services.AddScoped<ICombinedDeviceStateIdGetter, CombinedDeviceStateIdGetter>();
  }

  private static void BindViewModelFactories(this IServiceCollection services)
  {
    services.AddScoped<CommandDesignerFactory>();
    services.AddScoped<CommandParameterDesignerFactory>();
    services.AddScoped<ExperimentDesignerFactory>();
    services.AddScoped<MetadataPickerFactory>();
    services.AddScoped<ParameterEditorFactory>();
    services.AddScoped<PlannableParameterDesignerFactory>();
    services.AddScoped<StepDesignerFactory>();
    services.AddScoped<PlanningDesignerFactory>();
    services.AddScoped<DeviceStateFilterViewModelFactory>();
  }
}
