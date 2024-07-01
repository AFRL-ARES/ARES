using Ares.Messaging;
using Google.Protobuf.WellKnownTypes;

namespace Ares.Core.Analyzing;

public interface IAnalyzer
{
  /// <summary>
  /// Optional name for the analyzer (can be useful when multiple analyzers of same type and version have to be used)
  /// </summary>
  string Name { get; }

  /// <summary>
  /// Version of the analyzer
  /// </summary>
  Version Version { get; }

  /// <summary>
  /// Provides an observable for the <see cref="AnalyzerState" />
  /// </summary>
  IObservable<AnalyzerState> AnalyzerStateObservable { get; }

  /// <summary>
  /// Current state (<see cref="AnalyzerState" />) of the analyzer which essentially indicated
  /// whether or not this analyzer is currently available for analyzing
  /// </summary>
  AnalyzerState AnalyzerState { get; }

  bool InputSupported(string fullTypeName);

  /// <summary>
  /// Returns the values for the given parameter metadata
  /// </summary>
  /// <param name="input">The experiment output to analyze in the form of the <see cref="Any" /> proto message</param>
  /// <param name="cancellationToken"></param>
  /// <returns><see cref="Analysis" /> which has the result as well as the metadata about the analyzer.</returns>
  Task<Analysis> Analyze(Any input, CancellationToken cancellationToken);
}
