using Ares.Datamodel.Analyzing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;
internal class RemoteDeviceConfigEntityConfiguration : AresEntityTypeBaseConfiguration<RemoteDeviceConfig>
{
  public override void Configure(EntityTypeBuilder<RemoteDeviceConfig> builder)
  {
    base.Configure(builder);

    builder.ToTable("RemoteDevices");
  }
}
