using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Analyzer;

public class AnalysisOverviewEntityConfiguration : AresEntityTypeBaseConfiguration<AnalysisOverview>
{
  public override void Configure(EntityTypeBuilder<AnalysisOverview> builder)
  {
    base.Configure(builder);
    builder.Property(overview => overview.AnalyzerInfo).HasAnalyzerInfo();
    builder.Property(overview => overview.Objectives).HasObjectives();
  }
}
