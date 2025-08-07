using Ares.Datamodel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class PlannerTransactionEntityConfiguration : AresEntityTypeBaseConfiguration<PlannerTransaction>
{
  public override void Configure(EntityTypeBuilder<PlannerTransaction> builder)
  {
    base.Configure(builder);
    builder.ToTable("PlannerTransactions");

    builder.Navigation(transaction => transaction.Request).AutoInclude();
    builder.Navigation(transaction => transaction.Response).AutoInclude();

    builder.HasOne<CompletedExperiment>()
      .WithMany(completedExperiment => completedExperiment.PlannerTransactions);
  }
}
