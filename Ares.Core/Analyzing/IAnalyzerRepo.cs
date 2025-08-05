namespace Ares.Core.Analyzing;

public interface IAnalyzerRepo
{
  IEnumerable<IAnalyzer> AvailableAnalyzers { get; }

  /// <summary>
  /// Gets a named analyzer based on the given analyzer name/> object
  /// </summary>
  /// <param name="name">The name of the analyzer requested</param>
  /// <returns>The analyzer or null if none is found </returns>
  IAnalyzer? GetAnalyzerByName(string name);

  /// <summary>
  /// Gets a named analyzer based on the given analyzer id/> object
  /// </summary>
  /// <param name="id">The id of the analyzer requested</param>
  /// <returns>The analyzer or null if none is found </returns>
  IAnalyzer? GetAnalyzerById(string id);


  /// <summary>
  /// Adds an analyzer to the registry so that it can later be used by experiment execution
  /// </summary>
  /// <param name="analyzer">The analyzer to register</param>
  internal void AddAnalyzer(IAnalyzer analyzer);

  /// <summary>
  /// Removes an analyzer from the registry
  /// </summary>
  /// <param name="analyzer">The analyzer to remove</param>
  internal void RemoveAnalyzer(IAnalyzer analyzer);

  /// <summary>
  /// Removes an analyzer from the registry based on the id
  /// </summary>
  /// <param name="analyzerId">The id of the analyzer to be removed</param>
  internal void RemoveAnalyzer(string analyzerId);
}
