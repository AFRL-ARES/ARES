using Ares.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class AnalysisEntityConfiguration : AresEntityTypeBaseConfiguration<Analysis>
{
  public override void Configure(EntityTypeBuilder<Analysis> builder)
  {
    base.Configure(builder);
    builder.ToTable("Analyses");

    builder.HasOne(t => t.CompletedExperiment)
      .WithOne()
      .HasForeignKey<Analysis>("CompletedExperimentId");

    builder.HasOne(t => t.Analyzer)
      .WithOne()
      .HasForeignKey<AnalyzerInfo>("AnalysisId")
      .OnDelete(DeleteBehavior.Cascade);
  }
}
