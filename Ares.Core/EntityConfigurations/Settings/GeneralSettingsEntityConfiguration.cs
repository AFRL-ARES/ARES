using Ares.Datamodel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Settings;

internal class GeneralSettingsEntityConfiguration : AresEntityTypeBaseConfiguration<AresGeneralSettingsConfig>
{
  public override void Configure(EntityTypeBuilder<AresGeneralSettingsConfig> builder)
  {
    base.Configure(builder);
    builder.ToTable("AresGeneralSettingsConfig");
  }
}
