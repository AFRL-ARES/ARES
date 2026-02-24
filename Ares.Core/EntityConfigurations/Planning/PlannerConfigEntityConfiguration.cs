using Ares.Datamodel.Planning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Planning;

public class PlannerConfigEntityConfiguration : AresEntityTypeBaseConfiguration<PlannerConfig>
{
  public override void Configure(EntityTypeBuilder<PlannerConfig> builder)
  {
    base.Configure(builder);
    builder.ToTable("PlannerServices");
  }
}
