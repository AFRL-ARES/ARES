using Ares.Core;
using Ares.Core.Grpc;
using Ares.Services;
using AresService.Data;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Radzen;
using Serilog;
using System.CommandLine;
using System.Reflection;
using UI;
using UI.Infrastructure.Grpc;
using UI.Application.Notifications;
using UI.Infrastructure.Notifications;
using UI.Infrastructure.Persistence;
using UI.Components.Formatting;
using UI.Application.Settings;

#region Command Line Params

  // 1. Define the command-line interface
  var migrateOption = new Option<bool>(name: "--migrate")
  {
    Description = "Creates and/or updates the database"
  };

  var checkDbOption = new Option<bool>(name: "--check-database")
  {
    Description = "Checks if database exists and/or needs an update. Uses the exit code to return 0 if database is good, 1 if database is good but needs and update, 2 if database does not exist, 3 if other error.",
  };

  var rootCommand = new RootCommand("Ares Service")
  {
    migrateOption,
    checkDbOption
  };

  rootCommand.SetAction(async parseResult =>
  {
    var shouldMigrate = parseResult.GetValue(migrateOption);
    if(shouldMigrate)
    {
      await RunMigrationsAsync(args);
      return 0;
    }

    var checkUpdate = parseResult.GetValue(checkDbOption);
    if(checkUpdate)
    {
      return await CheckDatabase(args);
    }

    await RunWebAppAsync(args);

builder.Services.AddSingleton<IMessenger>(provider => new WeakReferenceMessenger());
builder.Services.AddScoped<IClientManager, ClientManager>();
builder.Services.LoadAresModules();
builder.Services.BindClients();
builder.Services.AddSingleton<INotificationReceivingService, NotificationReceivingService>();

  var parseResult = rootCommand.Parse(args);

  return await parseResult.InvokeAsync();

#endregion

#region Database Stuff

  static async Task<int> CheckDatabase(string[] args)
  {
    var host = Host.CreateApplicationBuilder(args);
    host.Configuration
      .AddJsonFile("appsettings.aresservice.json", optional: false, reloadOnChange: true)
      .AddJsonFile($"appsettings.aresservice.{host.Environment.EnvironmentName}.json", optional: true);
    
    try
    {
      ConfigureDatabaseServices(host.Services, host.Configuration);
      var app = host.Build();
      var settings = host.Configuration.Get<AppSettings>();
      var provider = settings.DatabaseProvider;
      var aresDbContextFactory = app.Services.GetRequiredService<IDbContextFactory<AresDbContext>>();

      var contextResult = 0;
      await using(var dbContext = await aresDbContextFactory.CreateDbContextAsync())
      {
        contextResult |= await CheckContext(dbContext);
      }

      return contextResult;
    }
    catch(Exception e)
    {
      Console.ForegroundColor = ConsoleColor.Red;
      Console.Error.WriteLine("Failed to check database");
      Console.Error.WriteLine(e);

      return 3;
    }
  }

  static async Task<int> CheckContext(DbContext context)
  {
    var canConnect = await context.Database.CanConnectAsync();
    if(!canConnect)
    {
      return 11;
    }

    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
    if(pendingMigrations.Any())
    {
      return 10;
    }
    ;

    return 0;
  }

  static async Task RunMigrationsAsync(string[] args)
  {
    Console.WriteLine("Running database migrations...");

    // Build a temporary host to get the services
    var host = Host.CreateApplicationBuilder(args);
    host.Configuration
      .AddJsonFile("appsettings.aresservice.json", optional: false, reloadOnChange: true)
      .AddJsonFile($"appsettings.aresservice.{host.Environment.EnvironmentName}.json", optional: true);
    
    try
    {
      ConfigureDatabaseServices(host.Services, host.Configuration);
      var app = host.Build();
      var settings = host.Configuration.Get<AppSettings>();
      var provider = settings.DatabaseProvider;
      provider = string.IsNullOrEmpty(provider) ? "Sqlite" : provider;
      // DbContext factory
      var aresDbContextFactory = app.Services.GetRequiredService<IDbContextFactory<AresDbContext>>();

      // Migrate AresDbContext
      await using(var dbContext = await aresDbContextFactory.CreateDbContextAsync())
      {
        EnsureSqliteDirectoryExists(dbContext);
        await dbContext.Database.MigrateAsync();
        Console.WriteLine($"AresDbContext migration completed successfully for {provider}.");
      }

      Console.WriteLine("All database migrations completed.");
    }
    catch(Exception ex)
    {
      Console.ForegroundColor = ConsoleColor.Red;
      Console.WriteLine($"An error occurred during migration: {ex.Message}");
      Console.ResetColor();
    }
  }
  
  static void ConfigureDatabaseServices(IServiceCollection services, ConfigurationManager configuration)
  {
    var sqlConnectionStrings = configuration.GetSection("ConnectionStrings");
    var provider = configuration.Get<AppSettings>().DatabaseProvider ?? "Sqlite";

    if(provider == "SqlServer")
    {
      services.AddDbContextFactory<ApplicationDbContext>(b =>
      {
        b.UseSqlServer(sqlConnectionStrings[provider]);
        b.EnableSensitiveDataLogging();
      });
    }
    else if(provider == "Sqlite")
    {
      services.AddDbContextFactory<ApplicationDbContext>(b =>
      {
        b.UseSqlite(sqlConnectionStrings[provider]);
        b.EnableSensitiveDataLogging();
      });
    }
    else if(provider == "Postgres")
    {
      services.AddDbContextFactory<ApplicationDbContext>(b =>
      {
        b.UseNpgsql(sqlConnectionStrings[provider]);
        b.EnableSensitiveDataLogging();
      });
    }
    else
    {
      throw new InvalidOperationException($"Unsupported database provider: {provider}. Available provider values: {string.Join(',', sqlConnectionStrings.AsEnumerable().Select(scs => scs.Key.Split(':').LastOrDefault()).Where(s => s != "ConnectionStrings"))}");
    }
  }
  
  static void EnsureSqliteDirectoryExists(DbContext context)
  {
    var connection = context.Database.GetDbConnection();
    if(connection is not SqliteConnection sqlite)
    {
      return;
    }

    var dataSource = sqlite.DataSource;
    var directory = Path.GetDirectoryName(dataSource);
    if(string.IsNullOrEmpty(directory) || Directory.Exists(directory))
    {
      return;
    }

    Directory.CreateDirectory(directory);
  }

#endregion

#region Exception Setup

static void SetupExceptionHandling()
{
  AppDomain.CurrentDomain.UnhandledException += (s, e) =>
    LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

  TaskScheduler.UnobservedTaskException += (s, e) =>
  {
    LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
    e.SetObserved();
  };
}

static void LogUnhandledException(Exception exception, string source)
{
  try
  {
    var assemblyName = Assembly.GetExecutingAssembly().GetName();
    var message = $"Unhandled exception in {assemblyName.Name} v{assemblyName.Version}\n{exception.Message}";
    ServerStatusHelper.ServerStatusSubject.OnNext(new ServerStatusResponse
    {
      ServerStatus = ServerStatus.Error, StatusMessage = message
    });
  }
  catch(Exception)
  {
    // Failsafe in case logging itself throws an error
  }
}

#endregion

#region Helpers

static void PopulateAresConfig(IConfiguration configuration)
{
  var basePath = configuration.Get<AppSettings>()?.AresDataPath ?? ".";
  basePath = Path.GetFullPath(basePath);
  AresConfig.ResultsPath = Path.Combine(basePath, AppSettings.ResultsFolder);
  AresConfig.TemplatePath = Path.Combine(basePath, AppSettings.TemplatesFolder);
  AresConfig.DevicesPath = Path.Combine(basePath, AppSettings.DevicesFolder);
  AresConfig.TagsPath = Path.Combine(basePath, AppSettings.ExperimentTagsFile);
}

static void OnStopping()
{
  ServerStatusHelper.ServerStatusSubject.OnNext(new ServerStatusResponse { ServerStatus = ServerStatus.Stopping, StatusMessage = "Server is stopping." });
}

static void OnStopped()
{
  ServerStatusHelper.ServerStatusSubject.OnNext(new ServerStatusResponse { ServerStatus = ServerStatus.Stopped, StatusMessage = "Server has been stopped." });
}

#endregion
  
static Task RunWebAppAsync(params string[] args)
{
  var builder = WebApplication.CreateBuilder(args);

  Log.Logger = new LoggerConfiguration()
    .CreateBootstrapLogger();
  
  var services = builder.Services;
  var configuration = builder.Configuration;

  configuration
    .AddJsonFile("appsettings.ui.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.ui.{builder.Environment.EnvironmentName}.json", optional: true);

  ConfigureDatabaseServices(services, configuration);
  
  services.AddDatabaseDeveloperPageExceptionFilter();

  services.AddRazorComponents().AddInteractiveServerComponents();
  services.AddCascadingAuthenticationState();
  services.AddRadzenComponents();
  services.Configure<RemoteServiceSettings>(configuration.GetSection(nameof(RemoteServiceSettings)));
  services.Configure<CertificateSettings>(configuration.GetSection(nameof(CertificateSettings)));

  services.AddSingleton<IMessenger>(provider => new WeakReferenceMessenger());
  services.AddScoped<IClientManager, ClientManager>();
  services.LoadAresModules();
  services.BindClients();
  services.AddSingleton<INotificationReceivingService, NotificationReceivingService>();

  services.AddOptions();
  services.AddAntiforgery();

  services.AddHostedService<ServiceStarter>();

  services.AddSerilog((services, lc) => lc
    .ReadFrom.Configuration(configuration)
    .ReadFrom.Services(services)
    .WriteTo.Console()
    .Enrich.FromLogContext());

  ConfigureDatabaseServices(services, configuration);
  
  services.AddTransient<IDbContextFactory<CoreDatabaseContext>>(p =>
    new CovariantCoreDbContextFactory<CoreDatabaseContext, AresDbContext>(p.GetRequiredService<IDbContextFactory<AresDbContext>>()));
  
  var app = builder.Build();

// Configure the HTTP request pipeline.
  if(app.Environment.IsDevelopment())
  {
    app.UseMigrationsEndPoint();
  }
  else
  {
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
  }

  app.UseStatusCodePagesWithReExecute("/404");

  app.UseHttpsRedirection();

  app.MapStaticAssets();

  app.UseRouting();
  app.MapCoreAresServices();
  app.MapAresServices();

  app.UseAntiforgery();
  app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
  var appLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
  appLifetime.ApplicationStopping.Register(OnStopping);
  appLifetime.ApplicationStopped.Register(OnStopped);


  app.Services.GetService<UnitCategoryHelper>();

  return app.RunAsync();
}
