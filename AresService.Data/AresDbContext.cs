using Ares.Core;
using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel;
using Google.Protobuf.WellKnownTypes;
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

  protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
  {
    // Protobuf Data Types
    configurationBuilder.Properties<AresStruct>().HaveConversion<AresValueConverters>();
    configurationBuilder.Properties<AresValue>().HaveConversion<AresValueConverter>();
    
    // Protobuf Schemas
    configurationBuilder.Properties<AresValueSchema>().HaveConversion<AresValueSchemaConverter>();
    configurationBuilder.Properties<AresStructSchema>().HaveConversion<AresStructSchemaConverter>();

    // Utilities
    configurationBuilder.Properties<Timestamp>().HaveConversion<AresTimestampConverter>();

    base.ConfigureConventions(configurationBuilder);
  }
}
