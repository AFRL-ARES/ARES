using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;
internal class DeviceInfoEntityConfiguration : AresEntityTypeBaseConfiguration<DeviceInfo>
{
    public override void Configure(EntityTypeBuilder<DeviceInfo> builder)
    {
        base.Configure(builder);
        builder.Property(b => b.SettingsSchema).HasDataSchema();

        builder.HasMany(b => b.Commands)
            .WithOne()
            .HasForeignKey("DeviceInfoId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(b => b.Commands).AutoInclude();
    }
}
