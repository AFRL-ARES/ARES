using Ares.Core.Device.State.Export.ExportStreamProviders;
using Ares.Datamodel.Device;
using Ares.Services;
using Google.Protobuf;
using Grpc.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ares.Core.Grpc.Services;

public class DeviceStateExportService : Ares.Services.DeviceStateExportService.DeviceStateExportServiceBase
{
  readonly IEnumerable<IDeviceStateExportStreamProvider> _exportProviders;
  public DeviceStateExportService(IEnumerable<IDeviceStateExportStreamProvider> exportProviders)
  {
    _exportProviders = exportProviders;
  }

  public override Task<DeviceStateResponse> GetStateExport(DeviceStateRequest request, ServerCallContext? context)
    => GetZippedStates(request.Filter);
  

  private Task<DeviceStateResponse> GetZippedStates(DeviceStateRequestFilter filter)
  {
    var provider = _exportProviders.OfType<ZippedStatesExportStreamProvider>().First();
    return GenerateStateResponse(filter, provider);
  }

  private static async Task<DeviceStateResponse> GenerateStateResponse(DeviceStateRequestFilter filter, IDeviceStateExportStreamProvider streamProvider)
  {
    var stream = await streamProvider.Export(filter);
    var byteString = await ByteString.FromStreamAsync(stream);
    return new DeviceStateResponse() { Data = byteString };
  }
}
