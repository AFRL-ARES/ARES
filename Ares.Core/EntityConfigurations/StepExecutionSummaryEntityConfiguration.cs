using Ares.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class StepExecutionSummaryEntityConfiguration : AresEntityTypeBaseConfiguration<StepExecutionSummary>
{
  public override void Configure(EntityTypeBuilder<StepExecutionSummary> builder)
  {
    base.Configure(builder);
    builder.ToTable("StepExecutionSummaries");

    builder.HasMany(result => result.CommandSummaries)
      .WithOne()
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(result => result.ExecutionInfo)
      .WithOne()
      .HasForeignKey<ExecutionInfo>("StepExecutionSummaryId")
      .OnDelete(DeleteBehavior.ClientCascade);

    //builder.HasOne<StepTemplate>()
    //  .WithMany()
    //  .HasForeignKey(result => result.StepId)
    //  .OnDelete(DeleteBehavior.ClientCascade);

    builder.Navigation(result => result.CommandSummaries).AutoInclude();
    builder.Navigation(result => result.ExecutionInfo).AutoInclude();
  }
}
