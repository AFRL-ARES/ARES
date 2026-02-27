using System.Threading;
using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Health.V1;

namespace Ares.Core.Grpc.Services;

public class HealthCheckService : Health.HealthBase
{
  public override Task<HealthCheckResponse> Check(HealthCheckRequest request, ServerCallContext? context)
  {
    // very basic response for now, if the server can be contacted, then we assume it's serving
    var response = new HealthCheckResponse
      { Status = HealthCheckResponse.Types.ServingStatus.Serving };

    return Task.FromResult(response);
  }

  public override async Task Watch(HealthCheckRequest request, IServerStreamWriter<HealthCheckResponse> responseStream, ServerCallContext? context)
  {
    var cancellationToken = context?.CancellationToken ?? CancellationToken.None;
    var response = new HealthCheckResponse
      { Status = HealthCheckResponse.Types.ServingStatus.Serving };

    while (!cancellationToken.IsCancellationRequested)
    {
      await responseStream.WriteAsync(response);
      await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
    }
  }
}
