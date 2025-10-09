using Ares.Core.EntityConfigurations;
using Ares.Messages.DeviceStates.SyringePump;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AresService.Data.EntityConfigurations;
internal class SyringePumpStateConfiguration : AresEntityTypeBaseConfiguration<SyringePumpState>
{
  public override void Configure(EntityTypeBuilder<SyringePumpState> builder)
  {
    base.Configure(builder);
    builder.ToTable("SyringePumpStates");

    builder.Property(b => b.Timestamp)
      .HasConversion(t => t.ToDateTime(), d => d.ToTimestampUtc());

    builder.Property(b => b.RateUnit)
      .HasConversion<string>();

    builder.Property(b => b.VolumeUnit)
      .HasConversion<string>();

    builder.Property(b => b.Status)
      .HasConversion<string>();
  }
}
