using Ares.Datamodel.Visualizing.Remote;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Visualization;

public class RemoteVisualizerConfigEntityConfiguration : AresEntityTypeBaseConfiguration<RemoteVisualizationConfig>
{
  public override void Configure(EntityTypeBuilder<RemoteVisualizationConfig> builder)
  {
    base.Configure(builder);
  }
}