using Ares.Core.EntityConfigurations;
using Ares.Messages.DeviceStates.Chiller;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AresService.Data.EntityConfigurations;

public class LaserChillerStateConfiguration : AresEntityTypeBaseConfiguration<ChillerState>
{
  public override void Configure(EntityTypeBuilder<ChillerState> builder)
  {
    base.Configure(builder);
    builder.ToTable("ChillerStates");

    builder.Property(b => b.Timestamp)
      .HasConversion(t => t.ToDateTime(), d => d.ToTimestampUtc());
  }
}
