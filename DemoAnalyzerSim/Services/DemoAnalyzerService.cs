using DemoAnalyzer;
using Grpc.Core;

namespace DemoAnalyzerSim.Services;
public class DemoAnalyzerService : DemoAnalyzerGrpc.DemoAnalyzerGrpcBase
{
  private const double _idealGrowth = 500;
  public override Task<AnalysisResponse> Analyze(AnalysisRequest request, ServerCallContext context)
  {
    var analysisResult = request.Growth - _idealGrowth;
    Console.WriteLine($"Received request of {request.Growth} growth, responding with {analysisResult}");
    return Task.FromResult(new AnalysisResponse { Value = analysisResult });
  }
}
