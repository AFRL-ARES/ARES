using Ares.Datamodel.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Execution.Commands;

internal class CommandMetadataEntityConfiguration : AresEntityTypeBaseConfiguration<CommandMetadata>
{
  public override void Configure(EntityTypeBuilder<CommandMetadata> builder)
  {
    base.Configure(builder);
    builder.HasMany(commandMetadata => commandMetadata.ParameterMetadatas)
      .WithOne()
      .OnDelete(DeleteBehavior.ClientCascade);

    builder.HasOne(commandMetadata => commandMetadata.OutputMetadata)
      .WithOne()
      .HasForeignKey<OutputMetadata>()
      .IsRequired();

    builder.Navigation(commandMetadata => commandMetadata.ParameterMetadatas)
      .AutoInclude();

    builder.Navigation(commandMetadata => commandMetadata.OutputMetadata)
      .AutoInclude();
  }
}
