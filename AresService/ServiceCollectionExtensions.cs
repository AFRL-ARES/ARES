using Ares.Core;
using Ares.Core.Device.State.Export.ExportStreamProviders;
using Ares.Core.Execution;
using Ares.Core.Grpc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AresService;

public static class ServiceCollectionExtensions
{
  public static void AddAres(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddSingleton<AresStarter>();
    services.AddAresCoreComponents();
    services.AddNotificationHandlers();

    services.AddSingleton<IExecutionSummaryHandler>(provider =>
      {
        var stateExporters = provider.GetServices<IDeviceStateExportStreamProvider>();
        return new ExperimentResultJsonHandler(stateExporters);
      });
  }
}
