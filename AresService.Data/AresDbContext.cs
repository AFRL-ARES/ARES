using Ares.Core;
using Ares.Datamodel;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace AresService.Data;

public class AresDbContext : CoreDatabaseContext
{
  public AresDbContext(DbContextOptions<AresDbContext> options) : base(options)
  {
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    DatabaseRuntimeEnvironment.DatabaseProvider = Database.ProviderName;
    var assembly = Assembly.GetAssembly(typeof(AresDbContext));
    if(assembly is null)
      return;

    modelBuilder.ApplyConfigurationsFromAssembly(assembly);
    base.OnModelCreating(modelBuilder);
  }
}
