using Ares.Core.Grpc.Services;
using AresService.Services.Authentication;
using AresService.Services.Devices;
using AresService.Services.DeviceStateLogging;
using AresService.Services.OperationalState;
using AresService.Services.UserManagement;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AresService;

public static class ServiceMapper
{
  public static void MapAresServices(this IEndpointRouteBuilder routeBuilder)
  {
    routeBuilder.MapGrpcService<MfcService>();
    routeBuilder.MapGrpcService<SyringePumpService>();
    routeBuilder.MapGrpcService<AuthenticationService>();
    routeBuilder.MapGrpcService<UserManagementService>();
    routeBuilder.MapGrpcService<Tc0304Service>();
    routeBuilder.MapGrpcService<HerkulexService>();
    routeBuilder.MapGrpcService<TubeFurnaceService>();
    routeBuilder.MapGrpcService<FlirCM3CameraService>();
    routeBuilder.MapGrpcService<TubeFurnaceStateService>();
    routeBuilder.MapGrpcService<StepperControllerService>();
    routeBuilder.MapGrpcService<StepperControllerStateService>();
    routeBuilder.MapGrpcService<MfcStateService>();
    routeBuilder.MapGrpcService<Tc0304StateService>();
    routeBuilder.MapGrpcService<LaserChillerStateService>();
    routeBuilder.MapGrpcService<SyringePumpStateService>();
    routeBuilder.MapGrpcService<ValveControllerService>();
    routeBuilder.MapGrpcService<VerdiV6LaserService>();
    routeBuilder.MapGrpcService<LaserChillerService>();
    routeBuilder.MapGrpcService<ChemyxPumpService>();
    routeBuilder.MapGrpcService<RestDeviceService>();
    routeBuilder.MapGrpcService<RestSerialDeviceService>();
    routeBuilder.MapGrpcService<DeviceStateExportService>();
    routeBuilder.MapGrpcService<AresScriptingService>();
  }
}
