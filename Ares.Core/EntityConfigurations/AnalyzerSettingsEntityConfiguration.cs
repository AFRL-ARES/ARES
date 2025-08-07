using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel.Analyzing;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;
internal class AnalyzerSettingsEntityConfiguration : AresEntityTypeBaseConfiguration<AnalyzerSettings>
{
  public override void Configure(EntityTypeBuilder<AnalyzerSettings> builder)
  {
    base.Configure(builder);

    builder.Property(p => p.Settings).HasAresStruct();
  }
}
