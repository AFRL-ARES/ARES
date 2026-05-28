using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Execution.Parameters;

internal class ParameterEntityConfiguration : AresEntityTypeBaseConfiguration<Parameter>
{
  public override void Configure(EntityTypeBuilder<Parameter> builder)
  {
    base.Configure(builder);
    builder.ToTable("Parameters");

    builder.HasOne(parameter => parameter.Metadata)
      .WithOne()
      .HasForeignKey<ParameterMetadata>("ParameterId")
      .OnDelete(DeleteBehavior.Cascade);

    builder.Property(parameter => parameter.SourcePersistence)
      .HasColumnName("Source")
      .HasParameterSource();

    builder.Ignore(parameter => parameter.LiteralSource);
    builder.Ignore(parameter => parameter.PlannedSource);
    builder.Ignore(parameter => parameter.EnvironmentSource);
    builder.Ignore(parameter => parameter.CommandVariableSource);

    builder.Navigation(parameter => parameter.Metadata)
      .AutoInclude();
  }
}
