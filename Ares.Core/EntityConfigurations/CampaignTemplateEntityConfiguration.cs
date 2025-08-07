using Ares.Datamodel.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class CampaignTemplateEntityConfiguration : AresEntityTypeBaseConfiguration<CampaignTemplate>
{
  public override void Configure(EntityTypeBuilder<CampaignTemplate> builder)
  {
    base.Configure(builder);
    builder.ToTable("CampaignTemplates");

    builder.HasIndex(template => template.Name).IsUnique();

    builder.HasMany(campaignTemplate => campaignTemplate.ExperimentTemplates)
      .WithOne()
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasMany(campaignTemplate => campaignTemplate.PlannerAllocations)
      .WithOne()
      .IsRequired();// remove requirement if planners should exist separately from campaign templates

    builder.HasMany(campaignTemplate => campaignTemplate.PlannableParameters)
      .WithOne()
      .OnDelete(DeleteBehavior.ClientCascade)
      .IsRequired(false);

    builder.Navigation(campaignTemplate => campaignTemplate.ExperimentTemplates)
      .AutoInclude();

    builder.Navigation(template => template.PlannableParameters)
      .AutoInclude();

    builder.Navigation(template => template.PlannerAllocations)
      .AutoInclude();
  }
}
