using Ares.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class CampaignExecutionStatusEntityConfiguration : AresEntityTypeBaseConfiguration<CampaignExecutionStatus>
{
  public override void Configure(EntityTypeBuilder<CampaignExecutionStatus> builder)
  {
    base.Configure(builder);
    builder.ToTable("CampaignExecutionStatuses");

    builder.HasMany(status => status.ExperimentExecutionStatuses)
      .WithOne()
      .OnDelete(DeleteBehavior.Cascade);
  }
}
