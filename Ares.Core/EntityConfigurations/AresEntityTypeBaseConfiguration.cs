using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

public abstract class AresEntityTypeBaseConfiguration<TAresCoreEntity> : IEntityTypeConfiguration<TAresCoreEntity> where TAresCoreEntity : class, IMessage
{

  public virtual void Configure(EntityTypeBuilder<TAresCoreEntity> builder)
  {
    var dateGetterFunctionSql = DetermineDateTimeMethod();

    builder
      .Property<string?>("UniqueId")
      .HasConversion(s => string.IsNullOrEmpty(s) ? default : Guid.Parse(s), guid => guid.ToString())
      .ValueGeneratedOnAdd();

    builder
      .Property<DateTime>("CreationTime")
      .ValueGeneratedOnAdd()
      .HasDefaultValueSql(dateGetterFunctionSql);

    builder
      .Property<DateTime>("LastModified")
      .ValueGeneratedOnAddOrUpdate()
      .HasDefaultValueSql(dateGetterFunctionSql);

    builder.HasKey("UniqueId");
  }

  private string DetermineDateTimeMethod()
  {
    var provider = DatabaseRuntimeEnvironment.DatabaseProvider;

    if(provider is null)
      return "NOW()";

    if(provider.Contains("Postgres", StringComparison.CurrentCultureIgnoreCase))
      return "NOW()";

    if(provider.Contains("Sqlite", StringComparison.CurrentCultureIgnoreCase))
      return "DATETIME('now')";

    if(provider.Contains("SqlServer", StringComparison.CurrentCultureIgnoreCase))
      return "getdate()";

    else
      return "NOW()";
  }
}
