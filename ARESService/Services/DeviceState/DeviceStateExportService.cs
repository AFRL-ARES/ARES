using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ares.Messages.DeviceState;
using AresService.DeviceStateExport.ExportStreamProviders;
using Google.Protobuf;
using Grpc.Core;

namespace AresService.Services.DeviceState;

public class DeviceStateExportService : StateExportService.StateExportServiceBase
{
  readonly IEnumerable<IDeviceStateExportStreamProvider> _exportProviders;
  public DeviceStateExportService(IEnumerable<IDeviceStateExportStreamProvider> exportProviders)
  {
    _exportProviders = exportProviders;
  }

  public override Task<StateResponse> GetStateExport(StateRequest request, ServerCallContext context)
  {
    return request.ExportType switch
    {
      ExportType.Unspecified => GetZippedStates(request.Filter),
      ExportType.Combined => GetCombinedStates(request.Filter),
      ExportType.Zipped => GetZippedStates(request.Filter),
      _ => throw new System.NotImplementedException(),
    };
  }

  private Task<StateResponse> GetZippedStates(StateRequestFilter filter)
  {
    var provider = _exportProviders.OfType<ZippedStatesExportStreamProvider>().First();
    return GenerateStateResponse(filter, provider);
  }

  private Task<StateResponse> GetCombinedStates(StateRequestFilter filter)
  {
    var provider = _exportProviders.OfType<CombinedDeviceStateExportStreamProvider>().First();
    return GenerateStateResponse(filter, provider);
  }

  private static async Task<StateResponse> GenerateStateResponse(StateRequestFilter filter, IDeviceStateExportStreamProvider streamProvider)
  {
    var stream = await streamProvider.Export(filter);
    var byteString = await ByteString.FromStreamAsync(stream.Stream);
    return new StateResponse() { Data = byteString };
  }
}
