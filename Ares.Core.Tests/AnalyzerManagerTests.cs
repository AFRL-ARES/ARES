using Ares.Core.Analyzing;
using Ares.Messaging;
using Google.Protobuf.WellKnownTypes;

namespace Ares.Core.Tests;

internal class AnalyzerManagerTests
{
  private IAnalyzerManager _analyzerManager = new AnalyzerManager(new AnalysisRepo());

  [SetUp]
  public void SetUp()
  {
    _analyzerManager = new AnalyzerManager(new AnalysisRepo());
  }

  [Test]
  public void Manager_Should_Get_Typed_Analyzer_By_Version()
  {
    var analyzer = new TempAnalyzer("Test", new Version(1, 0));
    _analyzerManager.RegisterAnalyzer(analyzer);
    var returnedAnalyzer = _analyzerManager.GetAnalyzer<TempAnalyzer>(new Version(1, 0));
    Assert.That(analyzer, Is.SameAs(returnedAnalyzer));
  }

  [Test]
  public void Manager_Should_Throw_When_No_Analyzer_By_Version()
  {
    Assert.Throws<KeyNotFoundException>(() => _analyzerManager.GetAnalyzer<TempAnalyzer>(new Version(1, 0)));
  }

  private class TempAnalyzer : IAnalyzer
  {
    public TempAnalyzer(string name, Version version)
    {
      Name = name;
      Version = version;
    }

    public string Name { get; }
    public Version Version { get; }
    public IObservable<AnalyzerState> AnalyzerStateObservable { get; }
    public AnalyzerState AnalyzerState { get; }

    public bool InputSupported(string fullTypeName)
      => throw new NotImplementedException();

    public Task<Analysis> Analyze(Any input, CancellationToken cancellationToken)
      => throw new NotImplementedException();
  }
}
