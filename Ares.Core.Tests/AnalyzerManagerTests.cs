using Ares.Core.Analyzing;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Analyzing.Remote;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Ares.Core.Tests;

internal class AnalyzerManagerTests
{
  private IAnalyzerRepo _analyzerRepo;


  [SetUp]
  public void SetUp()
  {
    var dbCtxFactory = new Mock<IDbContextFactory<CoreDatabaseContext>>();
    _analyzerRepo = new AnalyzerRepo();
  }

  private class TempAnalyzer : AnalyzerBase
  {
    public TempAnalyzer(string name, string version) : base(name, "TempAnalyzer", version)
    {
    }

    public override Task<AnalysisResponse> Analyze(AnalysisRequest request, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    public override Task<AnalysisResponse> Analyze(AnalysisRequest request, AresStruct settings, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    public override Task<AnalyzerCapabilities> GetCapabilities(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    public override Task<AresStructSchema> GetParameters(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }
  }
}
