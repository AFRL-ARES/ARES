using System.Threading.Tasks;
using Ares.Core.Analyzing;
using Ares.Datamodel;
using Ares.Services;
using Grpc.Core;

namespace Ares.Core.Grpc.Services;

class AnalysisService : AresAnalysisService.AresAnalysisServiceBase
{
  private readonly IAnalyzerRepo _analyzerRepo;

  public AnalysisService(IAnalyzerRepo analyzerRepo)
  {
    _analyzerRepo = analyzerRepo;
  }

  public override async Task<AnalyzerParametersResponse> GetAnalyzerParameters(AnalyzerParametersRequest request, ServerCallContext context)
  {
    var analyzer = _analyzerRepo.GetAnalyzerById(request.AnalyzerId) ?? _analyzerRepo.GetDefaultAnalyzer();

    var analysisSchema = await analyzer.GetParameters();
    var response = new AnalyzerParametersResponse
    {
      AnalysisSchema = analysisSchema
    };

    return response;
  }

  public override async Task<ValidationResult> ValidateInputs(InputValidationRequest request, ServerCallContext context)
  {
    var analyzer = _analyzerRepo.GetAnalyzerById(request.AnalyzerId) ?? _analyzerRepo.GetDefaultAnalyzer();

    var validation = await analyzer.ValidateInputs(request.InputSchema);

    var validationResult = new ValidationResult
    {
      Success = validation.Success
    };
    validationResult.Messages.AddRange(validation.Messages);

    return validationResult;
  }
}
