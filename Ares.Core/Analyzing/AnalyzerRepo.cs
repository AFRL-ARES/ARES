using System.Collections.ObjectModel;

namespace Ares.Core.Analyzing;

public class AnalyzerRepo : IAnalyzerRepo
{
  private readonly IList<IAnalyzer> _analyzerStore = [];

  public AnalyzerRepo()
  {
    var defaultAnalyzer = new NoneAnalyzer();
    AddAnalyzer(defaultAnalyzer);
  }

  public IAnalyzer GetDefaultAnalyzer()
  {
    return _analyzerStore.OfType<NoneAnalyzer>().First();
  }

  public IAnalyzer? GetAnalyzerByName(string name)
  {
    var analyzer = _analyzerStore.FirstOrDefault(analyzer => analyzer.Name == name);

    return analyzer;
  }

  public void AddAnalyzer(IAnalyzer analyzer)
  {
    var analyzerExists = _analyzerStore.Any(p => p == analyzer || (p.Name == analyzer.Name && p.Version == analyzer.Version && analyzer.Type == p.Type));
    if(analyzerExists)
      throw new InvalidOperationException($"Analyzer {analyzer.Name}{analyzer.Version} of type {analyzer.GetType().Name} already registered");

    _analyzerStore.Add(analyzer);
  }

  public void RemoveAnalyzer(IAnalyzer analyzer)
  {
    var analyzerExists = _analyzerStore.Any(p => p == analyzer || (p.Name == analyzer.Name && p.Version == analyzer.Version && analyzer.Type == p.Type));
    if(!analyzerExists)
      return;

    _analyzerStore.Remove(analyzer);
  }

  public IAnalyzer? GetAnalyzerById(string id)
  {
    var analyzer = _analyzerStore.FirstOrDefault(analyzer => analyzer.UniqueId == id);

    return analyzer;
  }

  public void RemoveAnalyzer(string analyzerId)
  {
    var analyzer = _analyzerStore.FirstOrDefault(analyzer => analyzer.UniqueId == analyzerId);
    if(analyzer is null)
    {
      return;
    }

    _analyzerStore.Remove(analyzer);
  }

  public IEnumerable<IAnalyzer> AvailableAnalyzers => new ReadOnlyCollection<IAnalyzer>(_analyzerStore);
}
