using Ares.Core.Grpc.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Ares.Core.Grpc;

public static class ServiceMapper
{
  public static void MapCoreAresServices(this IEndpointRouteBuilder routeBuilder)
  {
    routeBuilder.MapGrpcService<DevicesService>();
    routeBuilder.MapGrpcService<AresServerInfoService>();
    routeBuilder.MapGrpcService<AutomationService>();
    routeBuilder.MapGrpcService<HealthCheckService>();
    routeBuilder.MapGrpcService<PlanningService>();
    routeBuilder.MapGrpcService<ValidationService>();
  }
}
