using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UI.Infrastructure.Persistence.DesignTime;

/// <summary>
/// This factory is responsible for design time migration stuff.
/// So like `dotnet ef migrations add" and `dotnet ef database update`
/// Probably shouldn't use the database update directly though as it will put the database in the wrong
/// location. Use the --migrate on the ares service itself.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class BaseDesignFactory<T> : IDesignTimeDbContextFactory<T> where T : DbContext
{
  public T CreateDbContext(string[] args)
  {
    var provider = "Sqlite";

    for(var i = 0; i < args.Length - 1; i++)
    {
      if(args[i].Equals("--provider", StringComparison.OrdinalIgnoreCase))
      {
        provider = args[i + 1];
        break;
      }
    }

    var config = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.ui.json", optional: true)
        .Build();

    var optionsBuilder = new DbContextOptionsBuilder<T>();

    switch(provider)
    {
      case "Sqlite":
        var sqliteConn = config.GetConnectionString("Sqlite") ?? "Data Source=../Data/ares_database.db";
        optionsBuilder.UseSqlite(sqliteConn, b => b.MigrationsAssembly("AresService.Migrations.Sqlite"));
        break;

      case "Postgres":
        var pgConn = config.GetConnectionString("Postgres") ?? "Host=localhost;Database=ARES;Username=postgres;Password=postgres";
        optionsBuilder.UseNpgsql(pgConn, b => b.MigrationsAssembly("AresService.Migrations.Postgres"));
        break;

      case "SqlServer":
        var sqlConn = config.GetConnectionString("SqlServer") ?? "Server=(localdb)\\MSSQLLocalDB;Database=ARES;Trusted_Connection=True;";
        optionsBuilder.UseSqlServer(sqlConn, b => b.MigrationsAssembly("AresService.Migrations.SqlServer"));
        break;

      default:
        throw new InvalidOperationException($"Unknown provider: {provider}");
    }

    return (T)Activator.CreateInstance(typeof(T), optionsBuilder.Options)!;
  }
}
