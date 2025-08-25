using Ares.Datamodel.Planning;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

public class PlannerEntityConfiguration : AresEntityTypeBaseConfiguration<Planner>
{
  public override void Configure(EntityTypeBuilder<Planner> builder)
  {
    base.Configure(builder);
  }
}
