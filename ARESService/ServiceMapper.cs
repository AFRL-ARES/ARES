using ARESService.Services.Authentication;
using ARESService.Services.Devices;
using ARESService.Services.DeviceStateLogging;
using ARESService.Services.UserManagement;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace ARESService;

public static class ServiceMapper
{
  public static void MapARESServices(this IEndpointRouteBuilder routeBuilder)
  {
    routeBuilder.MapGrpcService<MfcService>();
    routeBuilder.MapGrpcService<SyringePumpService>();
    routeBuilder.MapGrpcService<AuthenticationService>();
    routeBuilder.MapGrpcService<UserManagementService>();
    routeBuilder.MapGrpcService<Tc0304Service>();
    routeBuilder.MapGrpcService<HerkulexService>();
    routeBuilder.MapGrpcService<TubeFurnaceService>();
    routeBuilder.MapGrpcService<TubeFurnaceStateLoggingService>();
    routeBuilder.MapGrpcService<StepperControllerService>();
    routeBuilder.MapGrpcService<StepperControllerStateLoggingService>();
    routeBuilder.MapGrpcService<MfcStateLoggingService>();
    routeBuilder.MapGrpcService<Tc0304StateLoggingService>();
    routeBuilder.MapGrpcService<SyringePumpStateLoggingService>();
    routeBuilder.MapGrpcService<ValveControllerService>();
  }
}
