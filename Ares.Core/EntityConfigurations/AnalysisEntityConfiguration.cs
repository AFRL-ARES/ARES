using Ares.Datamodel.Analyzing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class AnalysisEntityConfiguration : AresEntityTypeBaseConfiguration<Analysis>
{
  public override void Configure(EntityTypeBuilder<Analysis> builder)
  {
    base.Configure(builder);
    builder.ToTable("Analyses");
  }
}
