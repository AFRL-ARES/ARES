using Ares.Core.EntityConfigurations;
using Ares.Messages.DeviceStates.Tc0304;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AresService.EntityConfigurations;
internal class Tc0304StateConfiguration : AresEntityTypeBaseConfiguration<Tc0304State>
{
  public override void Configure(EntityTypeBuilder<Tc0304State> builder)
  {
    base.Configure(builder);
    builder.ToTable("Tc0304States");

    builder.Property(b => b.Timestamp)
      .HasConversion(t => t.ToDateTime(), d => d.ToTimestampUtc());
  }
}
