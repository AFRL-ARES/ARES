using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel.Analyzing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Analyzer;

internal class AnalysisResponseEntityConfiguration : AresEntityTypeBaseConfiguration<AnalysisResponse>
{
  public override void Configure(EntityTypeBuilder<AnalysisResponse> builder)
  {
    base.Configure(builder);
    builder.Property(a => a.Objectives).HasObjectives();
    builder.ToTable("AnalysisResponses");
  }
}
