using Ares.Datamodel.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class StepTemplateEntityConfiguration : AresEntityTypeBaseConfiguration<StepTemplate>
{
  public override void Configure(EntityTypeBuilder<StepTemplate> builder)
  {
    base.Configure(builder);
    builder.ToTable("StepTemplates");
    builder.HasMany(stepTemplate => stepTemplate.CommandTemplates)
      .WithOne()
      .IsRequired();

    builder.Navigation(stepTemplate => stepTemplate.CommandTemplates)
      .AutoInclude();
  }
}
