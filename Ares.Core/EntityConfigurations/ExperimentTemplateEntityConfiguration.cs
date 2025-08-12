using Ares.Datamodel.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class ExperimentTemplateEntityConfiguration : AresEntityTypeBaseConfiguration<ExperimentTemplate>
{
  public override void Configure(EntityTypeBuilder<ExperimentTemplate> builder)
  {
    base.Configure(builder);
    builder.ToTable("ExperimentTemplates");

    builder.HasMany(experimentTemplate => experimentTemplate.StepTemplates)
      .WithOne()
      .OnDelete(DeleteBehavior.Cascade);

    builder.Navigation(experimentTemplate => experimentTemplate.StepTemplates)
      .AutoInclude();
  }
}
