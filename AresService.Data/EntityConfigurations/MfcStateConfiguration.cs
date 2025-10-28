using Ares.Core.EntityConfigurations;
using Ares.Core.EntityConfigurations.Helpers;
using Ares.Messages.DeviceStates.Mfc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AresService.Data.EntityConfigurations;

internal class MfcStateConfiguration : AresEntityTypeBaseConfiguration<MfcState>
{
  public override void Configure(EntityTypeBuilder<MfcState> builder)
  {
    base.Configure(builder);
    builder.ToTable("MfcStates");

    builder.Property(b => b.StatusCodes)
      .HasConversion(
      sc => string.Join(',', sc),
      s => s.ToRepeatedField(','), new StringEnumerableComparer());

    builder.Property(b => b.Timestamp)
      .HasConversion(t => t.ToDateTime(), d => d.ToTimestampUtc());
  }
}
