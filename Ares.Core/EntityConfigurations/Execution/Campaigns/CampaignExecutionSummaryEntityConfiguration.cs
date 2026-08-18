using Ares.Datamodel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Execution.Campaigns;

internal class CampaignExecutionSummaryEntityConfiguration : AresEntityTypeBaseConfiguration<CampaignExecutionSummary>
{
  public override void Configure(EntityTypeBuilder<CampaignExecutionSummary> builder)
  {
    base.Configure(builder);
    builder.ToTable("CampaignExecutionSummaries");

    builder.HasMany(result => result.ExperimentSummaries)
      .WithOne()
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(result => result.ExecutionInfo)
      .WithOne()
      .HasForeignKey<ExecutionInfo>("CampaignExecutionSummaryId")
      .OnDelete(DeleteBehavior.ClientCascade);

    builder.Navigation(result => result.ExecutionInfo)
      .AutoInclude();

    builder.Navigation(result => result.ExperimentSummaries)
      .AutoInclude();

    builder.Navigation(result => result.StartupExecutionSummary)
      .AutoInclude();

    builder.Navigation(result => result.CloseoutExecutionSummary)
      .AutoInclude();
  }
}
