using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel.Planning;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

public class PlannerSettingsEntityConfiguration : AresEntityTypeBaseConfiguration<PlannerSettings>
{
  public override void Configure(EntityTypeBuilder<PlannerSettings> builder)
  {
    base.Configure(builder);
    builder.Property(p => p.Settings).HasAresStruct();
  }
}
