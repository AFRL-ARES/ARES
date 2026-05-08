using Ares.Datamodel.Planning;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Planning;

public class PlannerTransactionProvider : IPlannerTransactionProvider
{
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;

  public PlannerTransactionProvider(IDbContextFactory<CoreDatabaseContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public async Task<IEnumerable<PlannerTransaction>> GetPlanningTransactionsAsync(PlannerTransactionRequestFilter filter)
  {
    using var context = _dbContextFactory.CreateDbContext();
    var transactions = await context.PlannerTransactions
      .Where(t => t.PlannerId == filter.PlannerId
        && t.TimeResponseReceived <= filter.End
        && t.TimeRequestSent >= filter.Start)
      .ToListAsync();

    return transactions;
  }
}
