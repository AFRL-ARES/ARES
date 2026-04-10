using Ares.Datamodel.Templates;

namespace Ares.Core.Execution.Executors.Composers;

public interface ICampaignComposer
{
  ICampaignExecutor Compose(CampaignTemplate template);
}
