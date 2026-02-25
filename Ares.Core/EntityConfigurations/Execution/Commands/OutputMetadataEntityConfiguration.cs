using Ares.Datamodel.Templates;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Execution.Commands;

public class OutputMetadataEntityConfiguration : AresEntityTypeBaseConfiguration<OutputMetadata>
{
  public override void Configure(EntityTypeBuilder<OutputMetadata> builder)
  {
    base.Configure(builder);
  }
}
