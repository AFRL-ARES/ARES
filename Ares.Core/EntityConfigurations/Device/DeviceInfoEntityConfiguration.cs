using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Device;

internal class DeviceInfoEntityConfiguration : AresEntityTypeBaseConfiguration<DeviceInfo>
{
    public override void Configure(EntityTypeBuilder<DeviceInfo> builder)
    {
        base.Configure(builder);

        builder.HasMany(b => b.Commands)
            .WithOne()
            .HasForeignKey("DeviceInfoId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(b => b.Commands).AutoInclude();
    }
}
