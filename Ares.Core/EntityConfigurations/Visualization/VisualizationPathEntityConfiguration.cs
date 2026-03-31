using Ares.Datamodel.Visualizing.Local;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Visualization;

public class VisualizationPathEntityConfiguration : AresEntityTypeBaseConfiguration<VisualizationPath>
{
  public override void Configure(EntityTypeBuilder<VisualizationPath> builder)
  {
    base.Configure(builder);
  }
}
