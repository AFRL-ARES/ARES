using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Execution.Experiments;

internal class ExperimentTemplateEntityConfiguration : AresEntityTypeBaseConfiguration<ExperimentTemplate>
{
  public override void Configure(EntityTypeBuilder<ExperimentTemplate> builder)
  {
    base.Configure(builder);
    builder.ToTable("ExperimentTemplates");

    builder.Property(template => template.AnalyzerMaps).HasSerializedMap();

    builder.HasMany(experimentTemplate => experimentTemplate.StepTemplates)
      .WithOne()
      .OnDelete(DeleteBehavior.Cascade);

    builder.Navigation(experimentTemplate => experimentTemplate.StepTemplates)
      .AutoInclude();

    builder.Property(template => template.PlanObjectives).HasSerializedRepeatedField();
  }
}
