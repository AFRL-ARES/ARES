using Ares.Core;
using Ares.Core.Grpc;
using Ares.Services;
using AresService.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.CommandLine;
using System.Reflection;
using UI;
using UI.Application.Settings;

#region Command Line Params

// 1. Define the command-line interface
var migrateOption = new Option<bool>(name: "--migrate")
{
  Description = "Creates and/or updates the database"
};

var checkDbOption = new Option<bool>(name: "--check-database")
{
  Description = "Checks if database exists and/or needs an update. Uses exit code 0 if database is good, 10 if migrations are pending, 11 if database is unavailable, and 3 for other errors.",
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
    return await RunMigrationsAsync(args);
  }

  var checkUpdate = parseResult.GetValue(checkDbOption);
  if(checkUpdate)
  {
    return await CheckDatabase(args);
  }

  await RunWebAppAsync(args);

  return 0;
});

var parseResult = rootCommand.Parse(args);

return await parseResult.InvokeAsync();

#endregion

#region Database Stuff

static async Task<int> CheckDatabase(string[] args)
{
  var host = Host.CreateApplicationBuilder(args);
  host.Configuration
    .AddJsonFile("appsettings.ui.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.ui.{host.Environment.EnvironmentName}.json", optional: true);

  try
  {
    host.Services.ConfigureDatabaseServices(host.Configuration);
    var app = host.Build();
    var settings = host.Configuration.Get<AppSettings>();
    var provider = settings?.DatabaseProvider;
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

static async Task<int> RunMigrationsAsync(string[] args)
{
  Console.WriteLine("Running database migrations...");

  // Build a temporary host to get the services
  var host = Host.CreateApplicationBuilder(args);
  host.Configuration
    .AddJsonFile("appsettings.ui.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.ui.{host.Environment.EnvironmentName}.json", optional: true);

  try
  {
    host.Services.ConfigureDatabaseServices(host.Configuration);
    var app = host.Build();
    var settings = host.Configuration.Get<AppSettings>();
    var provider = settings?.DatabaseProvider;
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
    return 0;
  }
  catch(Exception ex)
  {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"An error occurred during migration: {ex.Message}");
    Console.ResetColor();
    return 3;
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
      ServerStatus = ServerStatus.Error,
      StatusMessage = message
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
  
  services.AddMergedHostServices(configuration);

  var app = builder.Build();
  PopulateAresConfig(configuration);
  SetupExceptionHandling();

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
  
  app.UseAresUiPipeline();
  app.MapAresUiEndpoints();

  return app.RunAsync();
}
