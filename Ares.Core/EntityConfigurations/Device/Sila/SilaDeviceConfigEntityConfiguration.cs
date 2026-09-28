using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Device.Sila;

internal class SilaDeviceConfigEntityConfiguration : AresEntityTypeBaseConfiguration<SilaDeviceConfig>
{
  public override void Configure(EntityTypeBuilder<SilaDeviceConfig> builder)
  {
    base.Configure(builder);
  }
}
