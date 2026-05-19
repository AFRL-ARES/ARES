using Ares.Datamodel.Analyzing;

namespace Ares.Core.Analyzing;

public interface IAnalyzerTransactionProvider
{
  Task<IEnumerable<AnalyzerTransaction>> GetAnalyzerTransactionsAsync(AnalyzerTransactionRequestFilter filter);  
}
