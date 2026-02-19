using Ares.Services;
using Google.Protobuf.WellKnownTypes;
using System.Security.Cryptography;

namespace UI.Infrastructure.Devices;

public class DeviceDriverSyncManager
{
  private readonly AresDeviceDriverService.AresDeviceDriverServiceClient _client;
  private readonly string _localPluginPath;

  public DeviceDriverSyncManager(AresDeviceDriverService.AresDeviceDriverServiceClient client, string localPath)
  {
    _client = client;
    _localPluginPath = localPath;
    Directory.CreateDirectory(_localPluginPath);
  }

  public async Task SyncDriversAsync()
  {
    var coreDriversResponse = await _client.GetAvailableDriversAsync(new Empty());
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
