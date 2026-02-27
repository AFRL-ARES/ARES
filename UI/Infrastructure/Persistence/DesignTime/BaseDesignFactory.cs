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

    var optionsBuilder = new DbContextOptionsBuilder<T>();

    switch(provider)
    {
      case "Sqlite":
        optionsBuilder.UseSqlite(
            "Data Source=Data/app.db", b => b.MigrationsAssembly("AresService.Migrations.Sqlite"));
        break;

      case "Postgres":
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=ares;Username=postgres;Password=postgres", b => b.MigrationsAssembly("AresService.Migrations.Postgres"));
        break;

      case "SqlServer":
        optionsBuilder.UseSqlServer(
            "Server=localhost;Database=Ares;Trusted_Connection=True;TrustServerCertificate=True;", b => b.MigrationsAssembly("AresService.Migrations.SqlServer"));
        break;

      default:
        throw new InvalidOperationException($"Unknown provider: {provider}");
    }

    return (T)Activator.CreateInstance(typeof(T), optionsBuilder.Options)!;
  }
}
