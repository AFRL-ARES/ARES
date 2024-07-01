using Ares.Messaging;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class CompletedExperimentEntityConfiguration : AresEntityTypeBaseConfiguration<CompletedExperiment>
{
  public override void Configure(EntityTypeBuilder<CompletedExperiment> builder)
  {
    base.Configure(builder);
    builder.ToTable("CompletedExperiments");

    builder.HasOne(experiment => experiment.Template)
      .WithOne()
      .HasForeignKey<ExperimentTemplate>("CompletedExperimentId")
      .OnDelete(DeleteBehavior.ClientCascade);

    builder.HasMany(experiment => experiment.PlannerTransactions)
      .WithOne()
      .OnDelete(DeleteBehavior.Cascade);

    builder.Navigation(experiment => experiment.PlannerTransactions)
      .AutoInclude();

    builder.Navigation(experiment => experiment.Template)
      .AutoInclude();

    builder.HasOne(experiment => experiment.Result)
      .WithOne()
      .HasForeignKey<Any>("CompletedExperimentId")
      .OnDelete(DeleteBehavior.ClientCascade);
  }
}
