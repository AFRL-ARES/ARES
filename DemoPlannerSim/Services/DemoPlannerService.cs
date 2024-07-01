using DemoPlanner;
using Grpc.Core;

namespace DemoPlannerSim.Services;
public class DemoPlannerService : DemoPlannerGrpc.DemoPlannerGrpcBase
{
  public override Task<PlanResultResponse> Plan(PlanRequest request, ServerCallContext context)
  {
    var response = new PlanResultResponse();
    Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~");
    Console.WriteLine($"Received plan request for {string.Join(',', request.Metadata.Select(m => m.Name))}");
    Console.WriteLine($"Previous analyses: {string.Join(',', request.Analyses.Select(a => a.Result))}");
    foreach (var meta in request.Metadata)
    {
      var latestAnalysis = request.Analyses.LastOrDefault();
      if (latestAnalysis is null)
      {
        response.PlanResults.Add(new PlanResult { Metadata = meta, Value = 100 });
      }
      else
      {
        response.PlanResults.Add(new PlanResult { Metadata = meta, Value = 100 - request.Analyses.Select(a => a.Result).Sum() });
      }
    }
    Console.WriteLine($"Responding with: {string.Join(',', response.PlanResults.Select(r => $"{r.Metadata.Name} = {r.Value}"))}");
    Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~");
    return Task.FromResult(response);
  }
}
