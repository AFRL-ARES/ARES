using Ares.Messaging;

namespace Ares.Core.Analyzing;

public interface IAnalyzerManager
{
  IEnumerable<IAnalyzer> AvailableAnalyzers { get; }

  /// <summary>
  /// Gets a analyzer from the registry
  /// </summary>
  /// <typeparam name="T">Type of analyzer that implements IAnalyzer</typeparam>
  /// <returns>Analyzer of the given type</returns>
  /// <exception cref="KeyNotFoundException">Thrown if the analyzer is not found</exception>
  T GetAnalyzer<T>() where T : IAnalyzer;

  /// <summary>
  /// Gets a analyzer with a specific version from the registry
  /// </summary>
  /// <param name="version">Specific version of the analyzer type to get</param>
  /// <typeparam name="T">Type of analyzer that implements IAnalyzer</typeparam>
  /// <returns>Analyzer of the given type and version</returns>
  /// <exception cref="KeyNotFoundException">Thrown if the analyzer is not found</exception>
  T GetAnalyzer<T>(Version version) where T : IAnalyzer;

  /// <summary>
  /// Gets a named analyzer with a specific version from the registry
  /// </summary>
  /// <param name="name">Name of the analyzer</param>
  /// <param name="version">Specific version of the analyzer type to get</param>
  /// <typeparam name="T">Type of analyzer that implements IAnalyzer</typeparam>
  /// <returns>Analyzer of the given type, version, and name</returns>
  /// <exception cref="KeyNotFoundException">Thrown if the analyzer is not found</exception>
  T GetAnalyzer<T>(string name, Version version) where T : IAnalyzer;

  /// <summary>
  /// Gets a analyzer from the registry
  /// </summary>
  /// <param name="type">The type name of the analyzer</param>
  /// <returns>Analyzer of the given type</returns>
  /// <exception cref="KeyNotFoundException">Thrown if the analyzer is not found</exception>
  IAnalyzer GetAnalyzer(string type);

  /// <summary>
  /// Gets a analyzer with a specific version from the registry
  /// </summary>
  /// <param name="type">The type name of the analyzer</param>
  /// <param name="version">Specific version of the analyzer type to get</param>
  /// <returns>Analyzer of the given type and version</returns>
  /// <exception cref="KeyNotFoundException">Thrown if the analyzer is not found</exception>
  IAnalyzer GetAnalyzer(string type, Version version);

  /// <summary>
  /// Gets a named analyzer with a specific version from the registry
  /// </summary>
  /// <param name="type">The type name of the analyzer</param>
  /// <param name="name">Name of the analyzer</param>
  /// <param name="version">Specific version of the analyzer type to get</param>
  /// <returns>Analyzer of the given type, version, and name</returns>
  /// <exception cref="KeyNotFoundException">Thrown if the analyzer is not found</exception>
  IAnalyzer GetAnalyzer(string type, string name, Version version);

  /// <summary>
  /// Gets a named analyzer of the latest version from the registry
  /// </summary>
  /// <param name="type">The type name of the analyzer</param>
  /// <param name="name">Name of the analyzer</param>
  /// <returns>Analyzer of the given type and name</returns>
  /// <exception cref="KeyNotFoundException">Thrown if the analyzer is not found</exception>
  IAnalyzer GetAnalyzer(string type, string name);

  /// <summary>
  /// Gets a named analyzer based on the given <see cref="AnalyzerInfo" /> object
  /// </summary>
  /// <param name="info"></param>
  /// <returns></returns>
  IAnalyzer? GetAnalyzer(AnalyzerInfo info);

  /// <summary>
  /// Adds an analyzer to the registry so that it can later be used by experiment execution
  /// </summary>
  /// <param name="analyzer">The analyzer to register</param>
  void RegisterAnalyzer(IAnalyzer analyzer);


  // TODO remove
  void StoreAnalysis(Analysis analysis);

  void ClearAnalyses();
}
