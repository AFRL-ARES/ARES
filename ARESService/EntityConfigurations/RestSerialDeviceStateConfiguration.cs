using Ares.Core.EntityConfigurations;
using Ares.Messages.DeviceStates.RestSerialDevice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AresService.EntityConfigurations;

public class RestSerialDeviceStateConfiguration : AresEntityTypeBaseConfiguration<RestSerialDeviceStateEntity>
{
  public override void Configure(EntityTypeBuilder<RestSerialDeviceStateEntity> builder)
  {
    base.Configure(builder);
    builder.ToTable("RestSerialDeviceStateEntities");

    builder.Property(b => b.Timestamp)
      .HasConversion(t => t.ToDateTime(), d => d.ToTimestampUtc());
  }
}
