using Ares.Datamodel.Automation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Automation;

internal class CustomCommandEntityConfiguration : AresEntityTypeBaseConfiguration<CustomCommand>
{
  public override void Configure(EntityTypeBuilder<CustomCommand> builder)
  {
    base.Configure(builder);
    builder.ToTable("CustomCommands");
  }
}
