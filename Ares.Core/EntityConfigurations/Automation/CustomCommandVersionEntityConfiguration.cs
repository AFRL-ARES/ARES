using Ares.Datamodel.Automation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Automation;

internal class CustomCommandVersionEntityConfiguration : AresEntityTypeBaseConfiguration<CustomCommandVersion>
{
  public override void Configure(EntityTypeBuilder<CustomCommandVersion> builder)
  {
    base.Configure(builder);
    builder.ToTable("CustomCommandVersions");

    builder.HasIndex(version => new { version.CustomCommandId, version.VersionNumber })
      .IsUnique();

    builder.HasOne<CustomCommand>()
      .WithMany()
      .HasForeignKey(version => version.CustomCommandId)
      .HasPrincipalKey("UniqueId")
      .IsRequired()
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasMany(version => version.InputParameters)
      .WithOne()
      .HasForeignKey("CustomCommandVersionId")
      .IsRequired()
      .OnDelete(DeleteBehavior.Cascade);

    builder.Navigation(version => version.InputParameters)
      .AutoInclude();
  }
}
