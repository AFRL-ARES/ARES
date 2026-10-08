using Ares.Datamodel.Visualizing;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Visualization;

public class VisualizerInfoEntityConfiguration : AresEntityTypeBaseConfiguration<VisualizerInfo>
{
  public override void Configure(EntityTypeBuilder<VisualizerInfo> builder)
  {
    base.Configure(builder);
  }
}