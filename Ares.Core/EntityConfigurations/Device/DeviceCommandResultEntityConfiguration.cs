using Ares.Datamodel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Device;

internal class DeviceCommandResultEntityConfiguration : AresEntityTypeBaseConfiguration<CommandResult>
{
  public override void Configure(EntityTypeBuilder<CommandResult> builder)
  {
    base.Configure(builder);
    builder.ToTable("DeviceCommandResults");
  }
}
