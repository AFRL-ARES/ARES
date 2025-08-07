using Ares.Datamodel.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

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

    builder.HasOne(parameter => parameter.PlanningMetadata)
      .WithMany();

    builder.HasOne(parameter => parameter.Value)
      .WithOne()
      .HasForeignKey<ParameterValue>("ParameterId")
      .OnDelete(DeleteBehavior.Cascade);

    builder.Navigation(parameter => parameter.Metadata)
      .AutoInclude();

    builder.Navigation(parameter => parameter.PlanningMetadata)
      .AutoInclude();

    builder.Navigation(parameter => parameter.Value)
      .AutoInclude();
  }
}
