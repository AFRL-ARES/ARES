using Ares.Datamodel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class ExperimentExecutionSummaryEntityConfiguration : AresEntityTypeBaseConfiguration<ExperimentExecutionSummary>
{
  public override void Configure(EntityTypeBuilder<ExperimentExecutionSummary> builder)
  {
    base.Configure(builder);
    builder.ToTable("ExperimentExecutionSummaries");
    builder.HasMany(result => result.StepSummaries)
      .WithOne()
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(result => result.ExperimentOverview)
      .WithOne()
      .HasForeignKey<ExperimentOverview>("ExperimentResultId");

    builder.HasOne(result => result.ExecutionInfo)
      .WithOne()
      .HasForeignKey<ExecutionInfo>("ExperimentResultId")
      .OnDelete(DeleteBehavior.ClientCascade);

    builder.Navigation(result => result.  ExperimentOverview)
      .AutoInclude();

    builder.Navigation(result => result.ExecutionInfo)
      .AutoInclude();

    builder.Navigation(result => result.StepSummaries)
      .AutoInclude();
  }
}
