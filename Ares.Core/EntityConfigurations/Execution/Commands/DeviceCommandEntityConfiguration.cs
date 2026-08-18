using Ares.Datamodel.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Execution.Commands;

internal class DeviceCommandEntityConfiguration : AresEntityTypeBaseConfiguration<DeviceCommand>
{
  public override void Configure(EntityTypeBuilder<DeviceCommand> builder)
  {
    base.Configure(builder);
    builder.ToTable("DeviceCommands");

    builder.HasOne(command => command.Metadata)
      .WithOne()
      .HasForeignKey<CommandMetadata>("DeviceCommandId")
      .OnDelete(DeleteBehavior.Cascade);

    builder.Navigation(command => command.Metadata)
      .AutoInclude();
  }
}
