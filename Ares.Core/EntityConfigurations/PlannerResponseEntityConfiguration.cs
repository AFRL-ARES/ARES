using Ares.Datamodel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class PlannerResponseEntityConfiguration : AresEntityTypeBaseConfiguration<PlannerResponse>
{
  public override void Configure(EntityTypeBuilder<PlannerResponse> builder)
  {
    base.Configure(builder);
    builder.ToTable("PlannerResponses");
  }
}
