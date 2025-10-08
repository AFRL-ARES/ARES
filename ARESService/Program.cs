using System;
using System.CommandLine;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Ares.Core;
using Ares.Core.Grpc;
using Ares.Datamodel;
using Ares.Services;
using AresService;
using AresService.Services.Authentication;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

public class Program
{
  public static async Task<int> Main(string[] args)
  {
    // 1. Define the command-line interface
    var migrateOption = new Option<bool>(
        name: "--migrate")
    {
      Description = "Creates and/or updates the database"
    };

    var rootCommand = new RootCommand("Ares Service")
    {
      migrateOption
    };

    rootCommand.SetAction(async parseResult =>
    {
      var shouldMigrate = parseResult.GetValue(migrateOption);
      if(shouldMigrate)
      {
        await RunMigrationsAsync(args);
      }
      else
      {
        await RunWebAppAsync(args);
      }
    });

    var parseResult = rootCommand.Parse(args);

    return await parseResult.InvokeAsync();
  }

  private static async Task RunMigrationsAsync(string[] args)
  {
    Console.WriteLine("Running database migrations...");

    // Build a temporary host to get the services
    var host = Host.CreateApplicationBuilder(args);
    ConfigureDatabaseServices(host.Services, host.Configuration);
    var app = host.Build();

    try
    {
      // Resolve both DbContext factories
      var aresDbContextFactory = app.Services.GetRequiredService<IDbContextFactory<AresDbContext>>();
      var aresIdentityContextFactory = app.Services.GetRequiredService<IDbContextFactory<AresIdentityContext>>();

      // Migrate AresDbContext
      await using(var dbContext = await aresDbContextFactory.CreateDbContextAsync())
      {
        EnsureSqliteDirectoryExists(dbContext);
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("AresDbContext migrations completed successfully.");
      }

      // Migrate AresIdentityContext
      await using(var dbContext = await aresIdentityContextFactory.CreateDbContextAsync())
      {
        EnsureSqliteDirectoryExists(dbContext);
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("AresIdentityContext migrations completed successfully.");
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

  // The original logic for running the web application
  private static async Task RunWebAppAsync(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);
    var configuration = builder.Configuration;
    var services = builder.Services;

    // Service Configuration
    services.AddGrpc(options => options.EnableDetailedErrors = true);
    services.Configure<TokensConfig>(configuration.GetSection(nameof(TokensConfig)));
    services.AddLogging(b => b.AddConsole());

    ConfigureDatabaseServices(services, configuration);

    services.AddTransient<IDbContextFactory<CoreDatabaseContext>>(p =>
        new CovariantCoreDbContextFactory<CoreDatabaseContext, AresDbContext>(p.GetRequiredService<IDbContextFactory<AresDbContext>>()));

    var identityBuilder = services.AddIdentityCore<AresUser>(o =>
        o.Password = new PasswordOptions
        {
          RequireDigit = false,
          RequiredLength = 6,
          RequiredUniqueChars = 0,
          RequireLowercase = false,
          RequireNonAlphanumeric = false,
          RequireUppercase = false
        });

    identityBuilder = new IdentityBuilder(identityBuilder.UserType, typeof(IdentityRole), identityBuilder.Services);
    identityBuilder.AddEntityFrameworkStores<AresIdentityContext>();
    identityBuilder.AddRoleValidator<RoleValidator<IdentityRole>>();
    identityBuilder.AddRoleManager<RoleManager<IdentityRole>>();
    identityBuilder.AddSignInManager<SignInManager<AresUser>>();
    identityBuilder.AddDefaultTokenProviders();

    var token = configuration.Get<AppSettings>()?.TokensConfig?.Key ?? "DefaultKey";
    var key = Encoding.ASCII.GetBytes(token);

    services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme).AddJwtBearer(o =>
    {
      o.RequireHttpsMetadata = false;
      o.SaveToken = true;
      o.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        RequireExpirationTime = false
      };
    }).AddCertificate(o =>
    {
      o.AllowedCertificateTypes = CertificateTypes.All;
      o.RevocationMode = X509RevocationMode.NoCheck;
    });

    services.AddAuthorizationBuilder()
      .AddPolicy("AresPolicy", b => b.RequireRole(Enum.GetNames<AresUserType>()));

    services.AddAres(configuration);
    services.AddTransient<UserInitializer>();
    services.AddTransient<JwtTokenGenerator>();

    var app = builder.Build();

    // Middleware Pipeline Configuration
    PopulateAresConfig(configuration);

    if(app.Environment.IsDevelopment())
    {
      app.UseDeveloperExceptionPage();
    }
    else
    {
      app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapCoreAresServices();
    app.MapAresServices();
    app.MapGet("/",
          async context =>
          {
            await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client...");
          });

    // Application Lifetime and Initialization
    var appLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    appLifetime.ApplicationStopping.Register(OnStopping);
    appLifetime.ApplicationStopped.Register(OnStopped);

    SetupExceptionHandling();

    using(var scope = app.Services.CreateScope())
    {
      try
      {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await roleManager.InitializeAsync();

      }
      catch(Exception e)
      {
        Console.Error.WriteLine(e);
      }

      var userInitializer = scope.ServiceProvider.GetRequiredService<UserInitializer>();
      await userInitializer.Init();

      var starter = scope.ServiceProvider.GetRequiredService<AresStarter>();
      _ = starter.Start();
    }

    await app.RunAsync();
  }

  // Extracted database configuration to a separate method to avoid code duplication
  private static void ConfigureDatabaseServices(IServiceCollection services, ConfigurationManager configuration)
  {
    var sqlConnectionStrings = configuration.GetSection("ConnectionStrings");
    var provider = configuration.Get<AppSettings>()!.DatabaseProvider;

    if(provider == "SqlServer")
    {
      services.AddDbContextFactory<AresDbContext>(b =>
      {
        b.UseSqlServer(sqlConnectionStrings["MSSQL"]);
        b.EnableSensitiveDataLogging();
      });
      services.AddDbContextFactory<AresIdentityContext>(b => b.UseSqlServer(sqlConnectionStrings["MSSQL"]), ServiceLifetime.Transient);
    }
    else if(provider == "Sqlite")
    {
      services.AddDbContextFactory<AresDbContext>(b =>
      {
        b.UseSqlite(sqlConnectionStrings["Sqlite"]);
        b.EnableSensitiveDataLogging();
      });
      services.AddDbContextFactory<AresIdentityContext>(b => b.UseSqlite(sqlConnectionStrings["Sqlite"]), ServiceLifetime.Transient);
    }
    else if(provider == "Postgres")
    {
      services.AddDbContextFactory<AresDbContext>(b =>
      {
        b.UseNpgsql(sqlConnectionStrings["Postgres"]);
        b.EnableSensitiveDataLogging();
      });
      services.AddDbContextFactory<AresIdentityContext>(b => b.UseNpgsql(sqlConnectionStrings["Postgres"]), ServiceLifetime.Transient);
    }
    else
    {
      throw new InvalidOperationException($"Unsupported database provider: {provider}");
    }
  }

  // =====================================================================
  // Helper methods
  // =====================================================================
  private static void PopulateAresConfig(IConfiguration configuration)
  {
    var basePath = configuration.Get<AppSettings>()?.AresDataPath ?? ".";
    AresConfig.ResultsPath = Path.Combine(basePath, AppSettings.ResultsFolder);
    AresConfig.TemplatePath = Path.Combine(basePath, AppSettings.TemplatesFolder);
    AresConfig.DevicesPath = Path.Combine(basePath, AppSettings.DevicesFolder);
    AresConfig.TagsPath = Path.Combine(basePath, AppSettings.ExperimentTagsFile);
  }

  private static void OnStopping()
  {
    ServerStatusHelper.ServerStatusSubject.OnNext(new ServerStatusResponse { ServerStatus = ServerStatus.Stopping, StatusMessage = "Server is stopping." });
  }

  private static void OnStopped()
  {
    ServerStatusHelper.ServerStatusSubject.OnNext(new ServerStatusResponse { ServerStatus = ServerStatus.Stopped, StatusMessage = "Server has been stopped." });
  }

  private static void SetupExceptionHandling()
  {
    AppDomain.CurrentDomain.UnhandledException += (s, e) =>
      LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

    TaskScheduler.UnobservedTaskException += (s, e) =>
    {
      LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
      e.SetObserved();
    };
  }

  private static void LogUnhandledException(Exception exception, string source)
  {
    try
    {
      var assemblyName = Assembly.GetExecutingAssembly().GetName();
      var message = $"Unhandled exception in {assemblyName.Name} v{assemblyName.Version}\n{exception.Message}";
      ServerStatusHelper.ServerStatusSubject.OnNext(new ServerStatusResponse { ServerStatus = ServerStatus.Error, StatusMessage = message });
    }
    catch(Exception)
    {
      // Failsafe in case logging itself throws an error
    }
  }

  private static void EnsureSqliteDirectoryExists(DbContext context)
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
}