using Ares.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

public class PlannerInfoEntityConfiguration : AresEntityTypeBaseConfiguration<PlannerInfo>
{
  public override void Configure(EntityTypeBuilder<PlannerInfo> builder)
  {
    base.Configure(builder);
    builder.ToTable("Planners");
  }
}
