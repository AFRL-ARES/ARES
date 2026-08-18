using Ares.Datamodel.Templates;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Execution.Parameters;

internal class ParameterMetadataEntityConfiguration : AresEntityTypeBaseConfiguration<ParameterMetadata>
{
  public override void Configure(EntityTypeBuilder<ParameterMetadata> builder)
  {
    base.Configure(builder);

    builder.HasMany(parameterMetadata => parameterMetadata.Constraints)
      .WithOne()
      .IsRequired();

    builder.Navigation(parameterMetadata => parameterMetadata.Constraints)
      .AutoInclude();
  }
}
