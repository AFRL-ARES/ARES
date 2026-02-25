using Ares.Datamodel.Analyzing;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Analyzer;
internal class AnalyzerSettingsEntityConfiguration : AresEntityTypeBaseConfiguration<AnalyzerSettings>
{
  public override void Configure(EntityTypeBuilder<AnalyzerSettings> builder)
  {
    base.Configure(builder);
  }
}
