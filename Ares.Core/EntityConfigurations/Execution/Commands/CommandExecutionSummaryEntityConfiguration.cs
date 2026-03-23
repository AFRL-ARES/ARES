using Ares.Datamodel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Execution.Commands;

internal class CommandExecutionSummaryEntityConfiguration : AresEntityTypeBaseConfiguration<CommandExecutionSummary>
{
  public override void Configure(EntityTypeBuilder<CommandExecutionSummary> builder)
  {
    base.Configure(builder);
    builder.ToTable("CommandExecutionSummaries");

    builder.HasOne(result => result.ExecutionInfo)
      .WithOne()
      .HasForeignKey<ExecutionInfo>("CommandExecutionSummaryId")
      .OnDelete(DeleteBehavior.ClientCascade);

    builder.HasOne(result => result.Result)
      .WithOne()
      .HasForeignKey<CommandResult>("CommandExecutionSummaryId")
      .OnDelete(DeleteBehavior.ClientCascade);

    builder.Navigation(result => result.ExecutionInfo).AutoInclude();
    builder.Navigation(result => result.Result).AutoInclude();
  }
}
