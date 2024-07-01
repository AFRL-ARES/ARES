using Ares.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class StepResultEntityConfiguration : AresEntityTypeBaseConfiguration<StepResult>
{
  public override void Configure(EntityTypeBuilder<StepResult> builder)
  {
    base.Configure(builder);
    builder.ToTable("StepResults");
    builder.HasMany(result => result.CommandResults)
      .WithOne()
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(result => result.ExecutionInfo)
      .WithOne()
      .HasForeignKey<ExecutionInfo>("StepResultId")
      .OnDelete(DeleteBehavior.ClientCascade);

    builder.HasOne<StepTemplate>()
      .WithMany()
      .HasForeignKey(result => result.StepId)
      .OnDelete(DeleteBehavior.ClientCascade);

    builder.Navigation(result => result.CommandResults).AutoInclude();
    builder.Navigation(result => result.ExecutionInfo).AutoInclude();
  }
}
