using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Analyzing.Remote;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ares.Core.Analyzing;

public class AnalysisHelper
{
  readonly IAnalyzerRepo _analyzerRepo;
  private readonly ILogger<AnalysisHelper> _logger;
  private readonly IDbContextFactory<CoreDatabaseContext> _dbContextFactory;

  public AnalysisHelper(IAnalyzerRepo analyzerRepo, ILogger<AnalysisHelper> logger, IDbContextFactory<CoreDatabaseContext> dbContextFactory)
  {
    _analyzerRepo = analyzerRepo;
    _logger = logger;
    _dbContextFactory = dbContextFactory;
  }

  public async Task<Analysis> Analyze(ExperimentTemplate template, ExperimentExecutionSummary experimentSummary, ExperimentExecutionSummary startupSummary, RequestMetadata metadata, CancellationToken cancellationToken)
  {
    try
    {
      var transaction = new AnalyzerTransaction();
      var analyzer = GetAnalyzer(template.AnalyzerId);
      _logger.LogInformation("Analysis requested using analyzer {AnalyzerName}", analyzer.Name);

      var combinedResult = AresStructHelper.AppendStruct(experimentSummary.ExperimentOverview.Result, startupSummary.ExperimentOverview.Result);
      var analyzerInputs = ExperimentOutputToAnalyzerInputs(combinedResult, template.AnalyzerMaps);

      var analysisRequest = new AnalysisRequest 
      { 
        Inputs = analyzerInputs, 
        Metadata = metadata, 
        Settings = analyzer.Settings 
      };

      //Populate Transaction Info
      transaction.UniqueId = Guid.NewGuid().ToString();
      transaction.AnalyzerId = analyzer.UniqueId;
      transaction.AnalyzerName = analyzer.Name;
      transaction.AnalyzerType = analyzer.Type;
      transaction.AnalysisRequest = analysisRequest;
      transaction.TimeRequestSent = DateTime.UtcNow.ToTimestamp();

      var analysis = await analyzer.Analyze(analysisRequest, cancellationToken);
      transaction.TimeResponseReceived = DateTime.UtcNow.ToTimestamp();
      transaction.AnalysisResponse = analysis;
      
      experimentSummary.ExperimentOverview.AnalysisOverview = new AnalysisOverview
      {
        UniqueId = Guid.NewGuid().ToString(),
        Result = analysis.Result,
        AnalyzerInfo = await analyzer.CreateAnalyzerInfo(),
        ExperimentOverviewId = experimentSummary.ExperimentOverview.UniqueId
      };

      _logger.LogInformation("Analysis completed {Result}", analysis.Result);
      await LogAnalyzerTransaction(transaction);
      return analysis;
    }
    catch(RpcException e)
    {
      if(e.InnerException is OperationCanceledException oce)
      {
        return new Analysis { Result = float.NaN, AnalysisOutcome = Outcome.Canceled, ErrorString = $" Analysis has been canceled" };
      }
      return new Analysis {Result = float.NaN, AnalysisOutcome = Outcome.Failure, ErrorString = $"Call to analyzer has failed: {e}" };
    }
    catch(Exception e)
    {
      return new Analysis { Result = float.NaN, AnalysisOutcome = Outcome.Failure, ErrorString = $"Call to analyzer has failed: {e}" };
    }

    
  }

  private IAnalyzer GetAnalyzer(string? analyzerId)
  {
    if(analyzerId is null)
    {
      var noneAnalyzer = _analyzerRepo.GetAnalyzerById(NoneAnalyzer.Id);
      if(noneAnalyzer is null)
      {
        throw new InvalidOperationException(
          "No analyzer provided and the default NONE analyzer was not found.");
      }

      return noneAnalyzer;
    }

    return _analyzerRepo
    .GetAnalyzerById(analyzerId) ?? throw new InvalidOperationException($"Could not find desired analyzer with id {analyzerId}");
  }

  private AresStruct ExperimentOutputToAnalyzerInputs(AresStruct experimentResult, MapField<string, string> analyzerMappings)
  {
    try
    {
      var mappedStruct = new AresStruct();
      // Analyzer mapping is [KeyThatAnalyzerExpects, UserDefinedExperimentOutputKey]
      foreach(var map in analyzerMappings)
      {
        var expResultValue = experimentResult.Fields[map.Value];
        mappedStruct.Fields[map.Key] = expResultValue;
      }

      return mappedStruct;
    }

    catch(Exception)
    {
      //TODO: Gracefully handle
      throw;
    }

  }

  private async Task LogAnalyzerTransaction(AnalyzerTransaction transaction)
  {
    var context = _dbContextFactory.CreateDbContext();
    await context.AnalyzerTransactions.AddAsync(transaction);
    await context.SaveChangesAsync();
  }
}
