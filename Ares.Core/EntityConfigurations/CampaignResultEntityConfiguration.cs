using Ares.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class CampaignResultEntityConfiguration : AresEntityTypeBaseConfiguration<CampaignResult>
{
  public override void Configure(EntityTypeBuilder<CampaignResult> builder)
  {
    base.Configure(builder);
    builder.ToTable("CampaignResults");
    builder.HasMany(result => result.ExperimentResults)
      .WithOne()
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(result => result.ExecutionInfo)
      .WithOne()
      .HasForeignKey<ExecutionInfo>("CampaignResultId")
      .OnDelete(DeleteBehavior.ClientCascade);

    //builder.HasOne<CampaignTemplate>()
    //  .WithMany()
    //  .HasForeignKey(result => result.CampaignId)
    //  .OnDelete(DeleteBehavior.ClientCascade);

    builder.Navigation(result => result.ExecutionInfo)
      .AutoInclude();

    builder.Navigation(result => result.ExperimentResults)
      .AutoInclude();
  }
}
