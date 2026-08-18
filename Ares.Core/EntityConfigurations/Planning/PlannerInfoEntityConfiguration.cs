using Ares.Datamodel.Planning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Planning;

public class PlannerInfoEntityConfiguration : AresEntityTypeBaseConfiguration<PlannerServiceInfo>
{
  public override void Configure(EntityTypeBuilder<PlannerServiceInfo> builder)
  {
    base.Configure(builder);

    builder
      .HasOne(p => p.Capabilities)
      .WithOne()
      .HasForeignKey<PlannerServiceCapabilities>("PlannerInfoId")
      .IsRequired()
      .OnDelete(DeleteBehavior.Cascade);

    builder.Navigation(p => p.Capabilities).AutoInclude();
  }
}
