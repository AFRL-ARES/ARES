using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Device;
internal class DeviceStateEntityConfiguration : AresEntityTypeBaseConfiguration<DeviceState>
{
  public override void Configure(EntityTypeBuilder<DeviceState> builder)
  {
    base.Configure(builder);
  }
}
