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

    builder.HasOne(campaignTemplate => campaignTemplate.StartupTemplate)
      .WithOne()
      .HasForeignKey<ExperimentTemplate>(st => st.UniqueId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(campaignTemplate => campaignTemplate.ExperimentTemplate)
      .WithOne()
      .HasForeignKey<ExperimentTemplate>(et => et.UniqueId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(campaignTemplate => campaignTemplate.CloseoutTemplate)
      .WithOne()
      .HasForeignKey<ExperimentTemplate>(ct => ct.UniqueId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasMany(campaignTemplate => campaignTemplate.PlannerAllocations)
      .WithOne()
      .IsRequired();

    builder.HasMany(campaignTemplate => campaignTemplate.PlannableParameters)
      .WithOne()
      .OnDelete(DeleteBehavior.ClientCascade)
      .IsRequired(false);

    builder.Navigation(campaignTemplate => campaignTemplate.StartupTemplate)
      .AutoInclude();

    builder.Navigation(campaignTemplate => campaignTemplate.ExperimentTemplate)
      .AutoInclude();

    builder.Navigation(campaignTemplate => campaignTemplate.CloseoutTemplate)
      .AutoInclude();

    builder.Navigation(template => template.PlannableParameters)
      .AutoInclude();

    builder.Navigation(template => template.PlannerAllocations)
      .AutoInclude();
  }
}
