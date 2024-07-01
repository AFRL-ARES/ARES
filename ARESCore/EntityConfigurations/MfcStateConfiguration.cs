using Ares.Core.EntityConfigurations;
using Ares.Messages.DeviceStates.Mfc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ARESCore.EntityConfigurations;

internal class MfcStateConfiguration : AresEntityTypeBaseConfiguration<MfcState>
{
  public override void Configure(EntityTypeBuilder<MfcState> builder)
  {
    base.Configure(builder);
    builder.ToTable("MfcStates");

    builder.Property(b => b.StatusCodes)
      .HasConversion(sc => string.Join(',', sc), s => s.ToRepeatedField(','));

    builder.Property(b => b.Timestamp)
      .HasConversion(t => t.ToDateTime(), d => d.ToTimestampUtc());
  }
}
