using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Device;

internal class WidgetDescriptorEntityConfiguration : AresEntityTypeBaseConfiguration<WidgetDescriptor>
{
  public override void Configure(EntityTypeBuilder<WidgetDescriptor> builder)
  {
    base.Configure(builder);
  }
}