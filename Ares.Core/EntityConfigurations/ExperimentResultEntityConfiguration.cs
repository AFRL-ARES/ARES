using Ares.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class ExperimentResultEntityConfiguration : AresEntityTypeBaseConfiguration<ExperimentResult>
{
  public override void Configure(EntityTypeBuilder<ExperimentResult> builder)
  {
    base.Configure(builder);
    builder.ToTable("ExperimentResults");
    builder.HasMany(result => result.StepResults)
      .WithOne()
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(result => result.CompletedExperiment)
      .WithOne()
      .HasForeignKey<CompletedExperiment>("ExperimentResultId");

    builder.HasOne(result => result.ExecutionInfo)
      .WithOne()
      .HasForeignKey<ExecutionInfo>("ExperimentResultId")
      .OnDelete(DeleteBehavior.ClientCascade);

    builder.Navigation(result => result.CompletedExperiment)
      .AutoInclude();

    builder.Navigation(result => result.ExecutionInfo)
      .AutoInclude();

    builder.Navigation(result => result.StepResults)
      .AutoInclude();
  }
}
