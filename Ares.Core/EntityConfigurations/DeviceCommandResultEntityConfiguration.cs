using Ares.Core.EntityConfigurations.Helpers;
using Ares.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class DeviceCommandResultEntityConfiguration : AresEntityTypeBaseConfiguration<DeviceCommandResult>
{
  public override void Configure(EntityTypeBuilder<DeviceCommandResult> builder)
  {
    base.Configure(builder);
    builder.ToTable("DeviceCommandResults");
    builder.Property(r => r.Result).HasAresStruct();
  }
}
