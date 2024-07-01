using Ares.Messaging;
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

    builder.HasOne(experimentTemplate => experimentTemplate.Analyzer)
      .WithOne()
      .HasForeignKey<AnalyzerInfo>("ExperimentTemplateId")
      .OnDelete(DeleteBehavior.ClientCascade);

    builder.Navigation(experimentTemplate => experimentTemplate.StepTemplates)
      .AutoInclude();

    builder.Navigation(experimentTemplate => experimentTemplate.Analyzer)
      .AutoInclude();
  }
}
