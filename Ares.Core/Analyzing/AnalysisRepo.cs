using System.Collections.ObjectModel;
using Ares.Messaging.Analyzing;

namespace Ares.Core.Analyzing;

// TODO: Just for testing, remove once added to an experiment result or something.
public class AnalysisRepo : Collection<Analysis>
{
  public void StoreAnalysis(Analysis analysis)
  {
    Add(analysis);
  }

  public void ClearAnalyses()
  {
    Clear();
  }
}
