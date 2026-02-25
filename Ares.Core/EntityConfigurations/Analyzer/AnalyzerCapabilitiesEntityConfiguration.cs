using Ares.Datamodel.Analyzing;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Analyzer;
internal class AnalyzerCapabilitiesEntityConfiguration : AresEntityTypeBaseConfiguration<AnalyzerCapabilities>
{
  public override void Configure(EntityTypeBuilder<AnalyzerCapabilities> builder)
  {
    base.Configure(builder);
  }
}
