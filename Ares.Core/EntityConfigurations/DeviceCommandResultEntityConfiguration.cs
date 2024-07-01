using Ares.Messaging;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class DeviceCommandResultEntityConfiguration : AresEntityTypeBaseConfiguration<DeviceCommandResult>
{
  public override void Configure(EntityTypeBuilder<DeviceCommandResult> builder)
  {
    base.Configure(builder);
    builder.ToTable("DeviceCommandResults");

    builder.HasOne(result => result.Result)
      .WithOne()
      .HasForeignKey<Any>("DeviceCommandResultId")
      .OnDelete(DeleteBehavior.ClientCascade);
  }
}
