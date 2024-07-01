using Ares.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class ExecutionInfoEntityConfiguration : AresEntityTypeBaseConfiguration<ExecutionInfo>
{
  public override void Configure(EntityTypeBuilder<ExecutionInfo> builder)
  {
    base.Configure(builder);
    builder.ToTable("ExecutionInfos");
    builder.Property(info => info.TimeStarted)
      .HasConversion(timestamp => timestamp.ToDateTime(), time => time.ToTimestampUtc());

    builder.Property(info => info.TimeFinished)
      .HasConversion(timestamp => timestamp.ToDateTime(), time => time.ToTimestampUtc());
  }
}
