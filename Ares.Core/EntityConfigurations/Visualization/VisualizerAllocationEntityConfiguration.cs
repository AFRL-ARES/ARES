using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel.Visualizing;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Visualization;

internal class VisualizerAllocationEntityConfiguration : AresEntityTypeBaseConfiguration<VisualizerAllocation>
{
  public override void Configure(EntityTypeBuilder<VisualizerAllocation> builder)
  {
    base.Configure(builder);
    builder.Property(b => b.VisualizationMaps).HasSerializedMap();
  }
}