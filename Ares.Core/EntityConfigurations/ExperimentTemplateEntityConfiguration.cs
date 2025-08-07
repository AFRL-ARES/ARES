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

    builder.HasMany(experimentTemplate => experimentTemplate.StartupStepTemplates)
      .WithOne()
      .OnDelete(DeleteBehavior.ClientCascade);

    builder.HasMany(experimentTemplate => experimentTemplate.CloseoutStepTemplates)
      .WithOne()
      .OnDelete(DeleteBehavior.ClientCascade);

    builder.Navigation(experimentTemplate => experimentTemplate.StepTemplates)
      .AutoInclude();

    builder.Navigation(experimentTemplate => experimentTemplate.StartupStepTemplates)
      .AutoInclude();

    builder.Navigation(experimentTemplate => experimentTemplate.CloseoutStepTemplates)
      .AutoInclude();
  }
}
