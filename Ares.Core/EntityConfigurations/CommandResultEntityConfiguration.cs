using Ares.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class CommandResultEntityConfiguration : AresEntityTypeBaseConfiguration<CommandResult>
{
  public override void Configure(EntityTypeBuilder<CommandResult> builder)
  {
    base.Configure(builder);
    builder.ToTable("CommandResults");
    builder.HasOne(result => result.ExecutionInfo)
      .WithOne()
      .HasForeignKey<ExecutionInfo>("CommandResultId")
      .OnDelete(DeleteBehavior.ClientCascade);

    builder.HasOne(result => result.Result)
      .WithOne()
      .HasForeignKey<DeviceCommandResult>("CommandResultId")
      .OnDelete(DeleteBehavior.ClientCascade);

    builder.HasOne<CommandTemplate>()
      .WithMany()
      .HasForeignKey(result => result.CommandId)
      .OnDelete(DeleteBehavior.ClientCascade);

    builder.Navigation(result => result.ExecutionInfo).AutoInclude();
    builder.Navigation(result => result.Result).AutoInclude();
  }
}
