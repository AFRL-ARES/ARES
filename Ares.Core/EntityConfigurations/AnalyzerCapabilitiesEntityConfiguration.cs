using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel.Analyzing;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;
internal class AnalyzerCapabilitiesEntityConfiguration : AresEntityTypeBaseConfiguration<AnalyzerCapabilities>
{
  public override void Configure(EntityTypeBuilder<AnalyzerCapabilities> builder)
  {
    base.Configure(builder);
    builder.Property(p => p.SettingsSchema).HasDataSchema();
  }
}
