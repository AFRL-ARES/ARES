using Ares.Datamodel.Planning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Planning;

public class PlannerCapabiltiesEntityConfiguration : AresEntityTypeBaseConfiguration<PlannerServiceCapabilities>
{
  public override void Configure(EntityTypeBuilder<PlannerServiceCapabilities> builder)
  {
    base.Configure(builder);

    builder.HasMany(service => service.AvailablePlanners)
      .WithOne()
      .OnDelete(DeleteBehavior.Cascade);

    builder.Navigation(service => service.AvailablePlanners).AutoInclude();
  }
}
