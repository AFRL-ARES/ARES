using Ares.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

public class PlannerInfoEntityConfiguration : AresEntityTypeBaseConfiguration<PlannerAdapterInfo>
{
  public override void Configure(EntityTypeBuilder<PlannerAdapterInfo> builder)
  {
    base.Configure(builder);
    builder.ToTable("Planners");
  }
}
