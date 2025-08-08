using Ares.Datamodel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class PlannerRequestEntityConfiguration : AresEntityTypeBaseConfiguration<PlannerRequest>
{
  public override void Configure(EntityTypeBuilder<PlannerRequest> builder)
  {
    base.Configure(builder);
    builder.ToTable("PlannerRequests");
    builder.HasMany(request => request.ParameterMetas)
      .WithOne()
      .OnDelete(DeleteBehavior.ClientCascade);
  }
}
