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
  }
}
