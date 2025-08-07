using Ares.Datamodel.Device;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class DeviceConfigEntityConfiguration : AresEntityTypeBaseConfiguration<DeviceConfig>
{
  public override void Configure(EntityTypeBuilder<DeviceConfig> builder)
  {
    base.Configure(builder);
    builder.ToTable("DeviceConfigs");

    builder.Navigation(config => config.ConfigData).AutoInclude();

    builder.HasOne(config => config.ConfigData)
      .WithOne()
      .HasForeignKey<Any>("DeviceConfigId")
      .OnDelete(DeleteBehavior.ClientCascade);
  }
}
