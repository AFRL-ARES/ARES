using Ares.Services;
using Ares.Core.Grpc.Services;
using Google.Protobuf.WellKnownTypes;
using System.Security.Cryptography;
using Grpc.Core;
using UI.Application.Devices.Repos;
using UI.Infrastructure.Grpc;

namespace UI.Infrastructure.Devices;

public class DeviceDriverSyncManager
{
  private readonly AresDriverService _client;
  private readonly IDeviceDriverRepository _driverRepository;
  private readonly string _localPluginPath;

  public DeviceDriverSyncManager(AresDriverService client, IDeviceDriverRepository driverRepository, string localPath)
  {
    _client = client;
    _driverRepository = driverRepository;
    _localPluginPath = localPath;
    Directory.CreateDirectory(_localPluginPath);
  }

  public async Task SyncDriversAsync()
  {
    var coreDriversResponse = await _client.GetAvailableDrivers(new Empty(), null);
    var coreDrivers = coreDriversResponse.Drivers.ToDictionary(d => d.DriverId);

    var localFiles = Directory.GetFiles(_localPluginPath, "*.dll");
    var localHashesToDrivers = localFiles.ToDictionary(f => ComputeHash(f));
    var staleDriverHashes = localHashesToDrivers.Keys.Where(hash => !coreDrivers.Values.Any(device => device.Checksum == hash));

    foreach(var hash in staleDriverHashes)
    {
      var staleDriver = localHashesToDrivers.GetValueOrDefault(hash) ?? string.Empty;
      Console.WriteLine($"Deleting Obsolete Driver: {staleDriver}");
      File.Delete(staleDriver);
    }

    var missingDriverIds = coreDrivers.Keys.Where(id => !localHashesToDrivers.ContainsKey(coreDrivers[id].Checksum));

    foreach(var id in missingDriverIds)
    {
      var driver = coreDrivers[id];
      var filePath = Path.Combine(_localPluginPath, $"{driver.DisplayName}.dll");
      Console.WriteLine($"Downloading Missing Driver: {driver.DisplayName}");
      // Stream the driver to disk
      using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
      var streamWriter = new LocalStreamWriter<FileChunk>(async chunk => 
      {
          await fileStream.WriteAsync(chunk.Content.ToByteArray());
      });
      await _client.DownloadDriver(new DriverRequest { DriverId = id }, streamWriter, null);
    }

    _driverRepository.Update(coreDrivers.Values.Select(d => d.DisplayName));
  }

  private string ComputeHash(string filePath)
  {
    if(!File.Exists(filePath)) return string.Empty;

    using(var sha256 = SHA256.Create())
    {
      // Important: Use FileShare.Read. 
      // This ensures you can calculate the hash even if the DLL 
      // is currently loaded/locked by the Service or UI process.
      using(var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
      {
        var hashBytes = sha256.ComputeHash(stream);

        // Convert byte array to a continuous hex string (e.g., "a3f5...")
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
      }
    }
  }
}
