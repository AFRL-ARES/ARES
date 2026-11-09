using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Device;

public class DeviceCommandDescriptorEntityConfiguration : AresEntityTypeBaseConfiguration<DeviceCommandDescriptor>
{
  public override void Configure(EntityTypeBuilder<DeviceCommandDescriptor> builder)
  {
    base.Configure(builder);
    builder.Property(descriptor => descriptor.WidgetDescriptors).HasSerializedRepeatedField();
  }
}