using Ares.Messaging;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ares.Core.EntityConfigurations;

internal class AresCampaignTagEntityConfiguration : AresEntityTypeBaseConfiguration<AresCampaignTag>
{
  public override void Configure(EntityTypeBuilder<AresCampaignTag> builder)
  {
    base.Configure(builder);
  }
}
