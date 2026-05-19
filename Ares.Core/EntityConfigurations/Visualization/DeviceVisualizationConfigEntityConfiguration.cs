using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel.Visualizing.Local;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Visualization;

public class DeviceVisualizationConfigEntityConfiguration : AresEntityTypeBaseConfiguration<DeviceVisualizationConfig>
{
  public override void Configure(EntityTypeBuilder<DeviceVisualizationConfig> builder)
  {
    base.Configure(builder);
    builder.Property(b => b.Paths).HasVisualizationPath();
  }
}
