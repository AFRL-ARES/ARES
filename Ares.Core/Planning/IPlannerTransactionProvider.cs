using Ares.Datamodel.Planning;

namespace Ares.Core.Planning;

public interface IPlannerTransactionProvider
{
  Task<IEnumerable<PlannerTransaction>> GetPlanningTransactionsAsync(PlannerTransactionRequestFilter filter);
}
