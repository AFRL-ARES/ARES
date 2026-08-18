using Ares.Datamodel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Execution.Experiments;

internal class ExperimentOverviewEntityConfiguration : AresEntityTypeBaseConfiguration<ExperimentOverview>
{
  public override void Configure(EntityTypeBuilder<ExperimentOverview> builder)
  {
    base.Configure(builder);
    builder.ToTable("ExperimentOverviews");

    builder.HasOne(experimentOverview => experimentOverview.AnalysisOverview)
      .WithOne()
      .OnDelete(DeleteBehavior.Cascade)
      .HasForeignKey<AnalysisOverview>(ao => ao.ExperimentOverviewId);

    builder.Navigation(e => e.AnalysisOverview).AutoInclude();
    builder.Navigation(e => e.Parameters).AutoInclude();
    builder.Navigation(e => e.Template).AutoInclude();
  }
}
