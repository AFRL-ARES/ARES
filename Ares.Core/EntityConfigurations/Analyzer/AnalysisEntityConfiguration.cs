using Ares.Datamodel.Analyzing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Analyzer;

#pragma warning disable CS0612 // Type or member is obsolete

internal class AnalysisEntityConfiguration : AresEntityTypeBaseConfiguration<Analysis>
{
  public override void Configure(EntityTypeBuilder<Analysis> builder)
  {
    base.Configure(builder);
    builder.ToTable("Analyses");
  }
}
