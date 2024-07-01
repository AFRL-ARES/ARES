using Ares.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class StepExecutionStatusEntityConfiguration : AresEntityTypeBaseConfiguration<StepExecutionStatus>
{
  public override void Configure(EntityTypeBuilder<StepExecutionStatus> builder)
  {
    base.Configure(builder);
    builder.ToTable("StepExecutionStatuses");

    builder.HasMany(status => status.CommandExecutionStatuses)
      .WithOne()
      .OnDelete(DeleteBehavior.Cascade);
  }
}
