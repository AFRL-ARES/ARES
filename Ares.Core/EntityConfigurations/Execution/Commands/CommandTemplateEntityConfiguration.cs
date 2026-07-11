using Ares.Datamodel.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Execution.Commands;

internal class CommandTemplateEntityConfiguration : AresEntityTypeBaseConfiguration<CommandTemplate>
{
  public override void Configure(EntityTypeBuilder<CommandTemplate> builder)
  {
    base.Configure(builder);
    builder.ToTable("CommandTemplates");
    builder.HasMany(commandTemplate => commandTemplate.ArgumentBindings)
      .WithOne()
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(template => template.DeviceCommand)
      .WithOne()
      .HasForeignKey<DeviceCommand>("CommandTemplateId")
      .OnDelete(DeleteBehavior.Cascade);
    builder.OwnsOne(template => template.SystemCommand);
    builder.OwnsOne(template => template.CustomCommandInvocation);

    builder.Navigation(template => template.DeviceCommand)
      .AutoInclude();

    builder.Navigation(commandTemplate => commandTemplate.ArgumentBindings)
      .AutoInclude();
  }
}
