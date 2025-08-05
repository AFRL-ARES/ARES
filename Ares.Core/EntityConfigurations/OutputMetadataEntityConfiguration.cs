using Ares.Core.EntityConfigurations.Helpers;
using Ares.Messaging;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

public class OutputMetadataEntityConfiguration : AresEntityTypeBaseConfiguration<OutputMetadata>
{
  public override void Configure(EntityTypeBuilder<OutputMetadata> builder)
  {
    base.Configure(builder);
    builder.Property(output => output.DataSchema).HasDataSchemaSimplified();
  }
}
