using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

public class DeviceCommandDescriptorEntityConfiguration : AresEntityTypeBaseConfiguration<DeviceCommandDescriptor>
{
    public override void Configure(EntityTypeBuilder<DeviceCommandDescriptor> builder)
    {
        base.Configure(builder);
        builder.Property(p => p.InputSchema).HasDataSchema();
        builder.Property(p => p.OutputSchema).HasDataSchema();
    }
}