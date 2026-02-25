using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Device;
internal class DeviceSettingsEntityConfiguration : AresEntityTypeBaseConfiguration<DeviceSettings>
{
  public override void Configure(EntityTypeBuilder<DeviceSettings> builder)
  {
    base.Configure(builder);
  }
}
