using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Device;

public class DeviceCommandDescriptorEntityConfiguration : AresEntityTypeBaseConfiguration<DeviceCommandDescriptor>
{
    public override void Configure(EntityTypeBuilder<DeviceCommandDescriptor> builder)
    {
        base.Configure(builder);
    }
}