using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

public abstract class AresEntityTypeBaseConfiguration<TAresCoreEntity> : IEntityTypeConfiguration<TAresCoreEntity> where TAresCoreEntity : class, IMessage
{
  public virtual void Configure(EntityTypeBuilder<TAresCoreEntity> builder)
  {
    const string dateGetterFunctionSql = "getdate()";

    // builder
    //   .Property<string?>("UniqueId")
    //   .HasConversion(s => string.IsNullOrEmpty(s) ? default : Guid.Parse(s), guid => guid.ToString())
    //   .ValueGeneratedOnAdd();

    builder
      .Property<string?>("UniqueId")
      .HasDefaultValueSql("NEWID()");

    builder
      .Property<DateTime>("CreationTime")
      .ValueGeneratedOnAdd()
      .HasDefaultValueSql(dateGetterFunctionSql);

    builder
      .Property<DateTime>("LastModified")
      .ValueGeneratedOnUpdate()
      .HasDefaultValueSql(dateGetterFunctionSql);

    builder.HasKey("UniqueId");
  }
}
