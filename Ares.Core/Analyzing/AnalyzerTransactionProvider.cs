using Ares.Datamodel.Analyzing;
using Microsoft.EntityFrameworkCore;

namespace Ares.Core.Analyzing;

public class AnalyzerTransactionProvider : IAnalyzerTransactionProvider
{
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;

  public AnalyzerTransactionProvider(IDbContextFactory<CoreDatabaseContext> dbContextFactory)
  {
    _dbContextFactory = dbContextFactory;
  }

  public async Task<IEnumerable<AnalyzerTransaction>> GetAnalyzerTransactionsAsync(AnalyzerTransactionRequestFilter filter)
  {
    using var context = _dbContextFactory.CreateDbContext();
    var transactions = await context.AnalyzerTransactions
      .Where(t => t.AnalyzerId == filter.AnalyzerId 
        && t.TimeResponseReceived <= filter.End 
        && t.TimeRequestSent >= filter.Start)
      .ToListAsync();

    return transactions;
  }
}
