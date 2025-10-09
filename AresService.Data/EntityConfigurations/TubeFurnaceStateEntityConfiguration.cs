using Ares.Core.EntityConfigurations;
using Ares.Messages.DeviceStates.TubeFurnace;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AresService.Data.EntityConfigurations;
internal class TubeFurnaceStateEntityConfiguration : AresEntityTypeBaseConfiguration<TubeFurnaceStateEntity>
{
  public override void Configure(EntityTypeBuilder<TubeFurnaceStateEntity> builder)
  {
    base.Configure(builder);
    builder.ToTable("TubeFurnaceStateEntities");

    builder.Property(b => b.Timestamp)
      .HasConversion(t => t.ToDateTime(), d => d.ToTimestampUtc());
  }
}
