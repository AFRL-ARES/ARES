using Ares.Services;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Device.Drivers;

public class DeviceDriverEntityConfiguration : AresEntityTypeBaseConfiguration<DriverInfo>
{
  public override void Configure(EntityTypeBuilder<DriverInfo> builder)
  {
    base.Configure(builder);
  }
}
