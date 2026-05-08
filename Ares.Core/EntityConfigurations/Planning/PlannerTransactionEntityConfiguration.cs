using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel.Planning;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Planning;

internal class PlannerTransactionEntityConfiguration : AresEntityTypeBaseConfiguration<PlannerTransaction>
{
  public override void Configure(EntityTypeBuilder<PlannerTransaction> builder)
  {
    base.Configure(builder);
    builder.Property(transaction => transaction.PlanningRequest).HasPlanningRequest();
    builder.Property(transaction => transaction.PlanningResponse).HasPlanningResponse();
  }
}
