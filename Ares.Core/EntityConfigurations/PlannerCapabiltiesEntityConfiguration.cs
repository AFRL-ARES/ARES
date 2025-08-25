using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel.Planning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

public class PlannerCapabiltiesEntityConfiguration : AresEntityTypeBaseConfiguration<PlannerServiceCapabilities>
{
  public override void Configure(EntityTypeBuilder<PlannerServiceCapabilities> builder)
  {
    base.Configure(builder);

    builder.HasMany(service => service.AvailablePlanners)
      .WithOne()
      .OnDelete(DeleteBehavior.Cascade);

    builder.Property(p => p.SettingsSchema).HasDataSchema();
    builder.Navigation(service => service.AvailablePlanners).AutoInclude();
  }
}
