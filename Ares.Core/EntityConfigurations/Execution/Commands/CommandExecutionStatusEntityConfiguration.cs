using Ares.Datamodel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Execution.Commands;

internal class CommandExecutionStatusEntityConfiguration : AresEntityTypeBaseConfiguration<CommandExecutionStatus>
{
  public override void Configure(EntityTypeBuilder<CommandExecutionStatus> builder)
  {
    base.Configure(builder);
    builder.ToTable("CommandExecutionStatuses");

    builder.Property(status => status.State)
      .HasConversion<string>();
  }
}
