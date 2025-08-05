using Ares.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class PlannerAllocationEntityConfiguration : AresEntityTypeBaseConfiguration<PlannerAllocation>
{
  public override void Configure(EntityTypeBuilder<PlannerAllocation> builder)
  {
    base.Configure(builder);
    builder.ToTable("PlannerAllocations");

    builder.HasOne(planner => planner.Planner)
      .WithMany()
      .HasForeignKey("PlannerId")
      .OnDelete(DeleteBehavior.ClientCascade);

    builder.Navigation(allocation => allocation.Parameter).AutoInclude();
    builder.Navigation(allocation => allocation.Planner).AutoInclude();
  }
}
