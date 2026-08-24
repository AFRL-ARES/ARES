using Ares.Datamodel.Automation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Automation;

internal class CustomCommandParameterEntityConfiguration : AresEntityTypeBaseConfiguration<CustomCommandParameter>
{
  public override void Configure(EntityTypeBuilder<CustomCommandParameter> builder)
  {
    base.Configure(builder);
    builder.ToTable("CustomCommandParameters");
  }
}
