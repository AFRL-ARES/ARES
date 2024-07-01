using Ares.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class ParameterValueEntityConfiguration : AresEntityTypeBaseConfiguration<ParameterValue>
{
  public override void Configure(EntityTypeBuilder<ParameterValue> builder)
  {
    base.Configure(builder);
    builder.ToTable("ParameterValues");
  }
}
