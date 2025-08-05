using Ares.Messaging.Analyzing;

namespace Ares.Core.Analyzing;
/// <summary>
/// This is responsible for the loading and management of the analyzers during the lifetime
/// of the application. 
/// * stores/loads analyzers from the database
/// * populates the analyzer repository
/// </summary>
public interface IRemoteAnalyzerManager
{
  Task LoadAnalyzers();

  Task CreateAnalyzer(string name, string url);

  Task RemoveAnalyzer(string analyzerId);

  Task UpdateAnalyzer(AnalyzerConfig config);

  Task UpdateAnalyzerSettings(AnalyzerSettings analyzerSettings);
}
