using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Settings;

internal class GeneralSettingsEntityConfiguration : AresEntityTypeBaseConfiguration<AresGeneralSettingsConfig>
{
  public override void Configure(EntityTypeBuilder<AresGeneralSettingsConfig> builder)
  {
    base.Configure(builder);
    builder.Property(b => b.CommandLatency).HasDuration();
    builder.Property(b => b.RetryCooldown).HasDuration();
    builder.Property(b => b.DisplayCompatabilityWarnings).HasDefaultValue(true);
    builder.Property(b => b.DisplayDataCollectionWidget).HasDefaultValue(true);

    builder.ToTable("AresGeneralSettingsConfig");
  }
}
