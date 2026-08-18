using Ares.Datamodel.Device;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Device;

internal class DeviceConfigEntityConfiguration : AresEntityTypeBaseConfiguration<DeviceConfig>
{
  public override void Configure(EntityTypeBuilder<DeviceConfig> builder)
  {
    base.Configure(builder);

    builder.Navigation(config => config.SerialInfo)
      .AutoInclude();
    
    builder.ToTable("DeviceConfigs");
  }
}
