using Ares.Core.EntityConfigurations;
using Ares.Core.EntityConfigurations.Helpers;
using Ares.Messages.DeviceStates.TicStepperController;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AresService.Data.EntityConfigurations;
internal class TicStepperControllerStateConfiguration : AresEntityTypeBaseConfiguration<TicStepperControllerState>
{
  public override void Configure(EntityTypeBuilder<TicStepperControllerState> builder)
  {
    base.Configure(builder);
    builder.ToTable("TicStepperControllerStates");

    builder.Property(b => b.Timestamp)
      .HasConversion(t => t.ToDateTime(), d => d.ToTimestampUtc());

    builder.Property(b => b.StatusMessages)
      .HasConversion(sa => string.Join(',', sa), s => s.ToRepeatedField(','), new StringEnumerableComparer());

    builder.Property(b => b.StepMode)
      .HasConversion<string>();
  }
}
