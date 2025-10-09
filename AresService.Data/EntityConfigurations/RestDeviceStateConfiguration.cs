using Ares.Core.EntityConfigurations;
using Ares.Messages.DeviceStates.RestDevice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AresService.Data.EntityConfigurations;

public class RestDeviceStateConfiguration : AresEntityTypeBaseConfiguration<RestDeviceStateEntity>
{
  public override void Configure(EntityTypeBuilder<RestDeviceStateEntity> builder)
  {
    base.Configure(builder);
    builder.ToTable("RestDeviceStateEntities");

    builder.Property(b => b.Timestamp)
      .HasConversion(t => t.ToDateTime(), d => d.ToTimestampUtc());
  }
}
