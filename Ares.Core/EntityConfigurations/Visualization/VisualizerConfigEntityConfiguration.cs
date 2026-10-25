using Ares.Datamodel.Visualizing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Visualization;

public class VisualizerConfigEntityConfiguration : AresEntityTypeBaseConfiguration<VisualizerConfig>
{
  public override void Configure(EntityTypeBuilder<VisualizerConfig> builder)
  {
    base.Configure(builder);
    builder.ToTable("Visualizers");
  }
}