using Ares.Datamodel.Planning;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Planning;

public class PlannerSettingsEntityConfiguration : AresEntityTypeBaseConfiguration<PlannerSettings>
{
  public override void Configure(EntityTypeBuilder<PlannerSettings> builder)
  {
    base.Configure(builder);
  }
}
