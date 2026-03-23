using AresScriptingService=Ares.Core.Grpc.Services.AresScriptingService;
using DeviceStateExportService=Ares.Core.Grpc.Services.DeviceStateExportService;

namespace UI.Infrastructure.Grpc;

public static class ServiceMapper
{
  public static void MapAresServices(this IEndpointRouteBuilder routeBuilder)
  {
    routeBuilder.MapGrpcService<DeviceStateExportService>();
    routeBuilder.MapGrpcService<AresScriptingService>();
  }
}
