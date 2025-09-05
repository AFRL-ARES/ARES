using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;
internal class DeviceSettingsEntityConfiguration : AresEntityTypeBaseConfiguration<DeviceSettings>
{
  public override void Configure(EntityTypeBuilder<DeviceSettings> builder)
  {
    base.Configure(builder);

    builder.Property(b => b.Settings).HasAresStruct();
  }
}
