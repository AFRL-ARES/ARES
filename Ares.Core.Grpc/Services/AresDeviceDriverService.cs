using Ares.Core.Device.Repos;
using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Ares.Core.Grpc.Services;

public class AresDriverService : AresDeviceDriverService.AresDeviceDriverServiceBase
{
  private readonly IDeviceDriverRepo _deviceDriverRepo;

  public AresDriverService(IDeviceDriverRepo deviceDriverRepo)
  {
    _deviceDriverRepo = deviceDriverRepo;
  }

  public override Task<DriverListResponse> GetAvailableDrivers(Empty request, ServerCallContext? context)
  {
    var response = new DriverListResponse();

    foreach(var driver in _deviceDriverRepo.GetAllDrivers())
    {
      var info = new FileInfo(driver.ModulePath);

      response.Drivers.Add(new DriverInfo
      {
        DriverId = driver.UniqueId,
        DisplayName = driver.Manifest.Name,
        Version = driver.Manifest.DriverVersion,
        FileSizeBytes = driver.DriverSize,
        Checksum = driver.CheckSum
      });
    }

    return Task.FromResult(response);
  }

  public override async Task DownloadDriver(DriverRequest request, IServerStreamWriter<FileChunk> responseStream, ServerCallContext? context)
  {
    var matchingDriver = _deviceDriverRepo.GetDriverById(request.DriverId);

    if(matchingDriver is null || !File.Exists(matchingDriver.ModulePath)) 
      throw new RpcException(new Status(StatusCode.NotFound, "Driver not found"));

    // Stream the file in 64kb chunks
    const int chunkSize = 64 * 1024;
    using var fileStream = File.OpenRead(matchingDriver.ModulePath);
    var buffer = new byte[chunkSize];
    int bytesRead;
    long totalRead = 0;

    while((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
    {
      await responseStream.WriteAsync(new FileChunk
      {
        Content = Google.Protobuf.ByteString.CopyFrom(buffer, 0, bytesRead),
        Offset = totalRead
      });
      totalRead += bytesRead;
    }
  }
}
