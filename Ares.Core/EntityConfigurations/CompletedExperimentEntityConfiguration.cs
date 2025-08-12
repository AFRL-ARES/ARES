using Ares.Core.EntityConfigurations.Helpers;
using Ares.Datamodel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class CompletedExperimentEntityConfiguration : AresEntityTypeBaseConfiguration<ExperimentOverview>
{
  public override void Configure(EntityTypeBuilder<ExperimentOverview> builder)
  {
    base.Configure(builder);
    builder.ToTable("ExperimentOverviews");

    builder.Property(experiment => experiment.Result).HasAresStruct();

  }
}
