using Ares.Datamodel;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations.Execution.Campaigns;

internal class AresCampaignTagEntityConfiguration : AresEntityTypeBaseConfiguration<AresCampaignTag>
{
  public override void Configure(EntityTypeBuilder<AresCampaignTag> builder)
  {
    base.Configure(builder);
  }
}
