using Ares.Core;
using Ares.Core.Device.State.Export.ExportStreamProviders;
using Ares.Core.Execution;
using Ares.Core.Grpc;
using Ares.Core.Grpc.Services;
using Ares.Core.Grpc.Services.Notifications;
using Ares.Core.Grpc.Services.Safety;
using Ares.Core.Visualization.ViewModels;
using AresService.Data;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Radzen;
using Serilog;
using UI.Application.Devices.Repos;
using UI.Application.Dialog;
using UI.Application.Handlers;
using UI.Application.Notifications;
using UI.Application.Scripting;
using UI.Application.Settings;
using UI.Components.Formatting;
using UI.Features.Analyzing.Settings;
using UI.Features.Auth;
using UI.Features.CampaignEdit;
using UI.Features.CampaignEdit.Factories;
using UI.Features.CampaignEdit.ViewModels;
using UI.Features.Devices;
using UI.Features.Devices.Plugin;
using UI.Features.Devices.Remote.Factory;
using UI.Features.Devices.Sila;
using UI.Features.DeviceStateExport;
using UI.Features.DeviceStateLogging;
using UI.Features.DeviceStateLogging.Settings;
using UI.Features.Notifications;
using UI.Features.Planning.Settings;
using UI.Features.ServerHealth;
using UI.Features.Visualization.ViewModels;
using UI.Infrastructure.Auth;
using UI.Infrastructure.Devices;
using UI.Infrastructure.Dialog;
using UI.Infrastructure.Grpc;
using UI.Infrastructure.Monaco.Interops;
using UI.Infrastructure.Notifications;
using UI.Infrastructure.Startup;
using CampaignDesignerViewModel = UI.Features.CampaignEdit.ViewModels.CampaignDesignerViewModel;
using ExperimentExecutionDetailsViewModel = UI.Features.ExecutionHistory.ExperimentExecutionDetailsViewModel;
using ExecutionHistoryViewModel = UI.Features.ExecutionHistory.ExecutionHistoryViewModel;
using ExecutionViewModel = UI.Features.Execution.ExecutionViewModel;
using ManualPlannerViewModel = UI.Features.Execution.Planning.ManualPlannerViewModel;
using RemoteDeviceSettingsListViewModel = UI.Features.Devices.Remote.RemoteDeviceSettingsListViewModel;
using ScriptPlaygroundViewModel = UI.Features.ScriptPlayground.ScriptPlaygroundViewModel;
using UI.Features.Settings;

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
    services.AddSingleton<IDeviceControlViewModelRepo, DeviceControlViewModelRepo>();
    services.AddSingleton<INotificationRepository, NotificationRepository>();

    services.AddSingleton<IDeviceAdapterRepository, DeviceAdapterRepository>();
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
    //Ares Services
    services.AddScoped<AresServerInfoService>();
    services.AddScoped<AutomationService>();
    services.AddScoped<HealthCheckService>();
    services.AddScoped<PlannerService>();
    services.AddScoped<ValidationService>();
    services.AddScoped<AnalyzerService>();
    services.AddScoped<AnalysisService>();
    services.AddScoped<AresSafetyManagementService>();
    services.AddScoped<DeviceStateExportService>();
    services.AddSingleton<AresNotificationService>();
    services.AddSingleton<AresScriptingService>();
    services.AddSingleton<AresDriverService>();

    //Device Services
    services.AddSingleton<DevicesService>();
  }

  private static void BindViewModels(this IServiceCollection services)
  {
    services.AddScoped<ExperimentExecutionDetailsViewModel>();
    services.AddScoped<NotificationHistoryViewModel>();
    services.AddScoped<ProfileViewModel>();
    services.AddTransient<CampaignDesignerViewModel>();
    services.AddScoped<CampaignListViewModel>();
    services.AddScoped<ExecutionHistoryViewModel>();
    services.AddScoped<ExecutionViewModel>();
    services.AddScoped<ScriptPlaygroundViewModel>();
    services.AddScoped<VisualizationViewModel>();
    services.AddScoped<VisualizationSidebarViewModel>();

    //Device Settings List View Models
    services.AddTransient<AnalyzerSettingsListViewModel>();
    services.AddTransient<PlannerSettingsListViewModel>();
    services.AddTransient<RemoteDeviceSettingsListViewModel>();
    services.AddTransient<PluginDeviceSettingsListViewModel>();
    services.AddTransient<SystemSettingsViewModel>();
    services.AddTransient<SilaDeviceSettingsListViewModel>();

    //Other View Models
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
    services.AddSingleton<IAresDeviceViewModelFactory,  AresDeviceViewModelFactory>();
    services.AddSingleton<IRemoteDeviceControlViewModelFactory, RemoteDeviceControlViewModelFactory>();
  }

  public static void LoadService(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddAresCoreComponents();
    services.AddNotificationHandlers();

    services.AddSingleton<IExecutionSummaryHandler>(provider =>
    {
      var stateExporters = provider.GetServices<IDeviceStateExportStreamProvider>();
      return new ExperimentResultJsonHandler(stateExporters);
    });
  }

  public static void ConfigureDatabaseServices(this IServiceCollection services, IConfiguration configuration)
  {
    var sqlConnectionStrings = configuration.GetSection("ConnectionStrings");
    var provider = configuration.Get<AppSettings>()?.DatabaseProvider ?? "Sqlite";

    if(provider == "SqlServer")
    {
      services.AddDbContextFactory<AresDbContext>(b =>
      {
        b.UseSqlServer(sqlConnectionStrings[provider], builder => builder.MigrationsAssembly("AresService.Migrations.SqlServer"));
        b.EnableSensitiveDataLogging();
      });
    }
    else if(provider == "Sqlite")
    {
      services.AddDbContextFactory<AresDbContext>(b =>
      {
        b.UseSqlite(sqlConnectionStrings[provider], builder => builder.MigrationsAssembly("AresService.Migrations.Sqlite"));
        b.EnableSensitiveDataLogging();
      });
    }
    else if(provider == "Postgres")
    {
      services.AddDbContextFactory<AresDbContext>(b =>
      {
        b.UseNpgsql(sqlConnectionStrings[provider], builder => builder.MigrationsAssembly("AresService.Migrations.Postgres"));
        b.EnableSensitiveDataLogging();
      });
    }
    else
    {
      throw new InvalidOperationException($"Unsupported database provider: {provider}. Available provider values: {string.Join(',', sqlConnectionStrings.AsEnumerable().Select(scs => scs.Key.Split(':').LastOrDefault()).Where(s => s != "ConnectionStrings"))}");
    }
  }

  public static void AddMergedHostServices(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddGrpc(options => options.EnableDetailedErrors = true);
    services.AddDatabaseDeveloperPageExceptionFilter();

    services.AddRazorComponents().AddInteractiveServerComponents();
    services.AddCascadingAuthenticationState();
    services.AddRadzenComponents();
    services.Configure<RemoteServiceSettings>(configuration.GetSection(nameof(RemoteServiceSettings)));
    services.Configure<CertificateSettings>(configuration.GetSection(nameof(CertificateSettings)));

    services.AddSingleton<IMessenger>(_ => new WeakReferenceMessenger());
    services.AddScoped<IClientManager, ClientManager>();
    services.LoadAresModules();
    services.BindClients();
    services.AddSingleton<INotificationReceivingService, NotificationReceivingService>();
    services.AddSingleton<StartupStateTracker>();
    services.LoadService(configuration);

    services.AddOptions();
    services.AddAntiforgery();
    services.AddHostedService<ServiceStarter>();

    services.AddSerilog((services, lc) => lc
      .ReadFrom.Configuration(configuration)
      .ReadFrom.Services(services)
      .WriteTo.Console()
      .Enrich.FromLogContext());

    services.ConfigureDatabaseServices(configuration);
    services.AddTransient<IDbContextFactory<CoreDatabaseContext>>(p =>
      new CovariantCoreDbContextFactory<CoreDatabaseContext, AresDbContext>(p.GetRequiredService<IDbContextFactory<AresDbContext>>()));
  }
}
