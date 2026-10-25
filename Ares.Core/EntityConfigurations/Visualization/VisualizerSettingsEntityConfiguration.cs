using Ares.Datamodel.Analyzing;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Visualization;

public class VisualizerSettingsEntityConfiguration : AresEntityTypeBaseConfiguration<VisualizerSettings>
{
  public override void Configure(EntityTypeBuilder<VisualizerSettings> builder)
  {
    base.Configure(builder);
    builder.HasIndex(settings => settings.VisualizerId).IsUnique();
  }
}