using Ares.Datamodel.Analyzing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

public class AnalyzerConfigEntityConfiguration : AresEntityTypeBaseConfiguration<AnalyzerConfig>
{
  public override void Configure(EntityTypeBuilder<AnalyzerConfig> builder)
  {
    base.Configure(builder);
    builder.ToTable("Analyzers");
  }
}
