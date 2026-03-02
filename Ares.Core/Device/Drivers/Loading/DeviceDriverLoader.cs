using Ares.Core.Device.Manifest;
using Ares.Core.Device.Repos;
using Ares.Device;
using Ares.Toolkit.Device.UI;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Ares.Core.Device.Drivers.Loading;

public class DeviceDriverLoader : IDeviceDriverLoader
{
  private readonly IDeviceDriverRepo _driverRepo;
  private readonly IDeserializer _deserializer;

  public DeviceDriverLoader(IDeviceDriverRepo driverRepo)
  {
    _driverRepo = driverRepo;
    _deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();
  }

  public async Task LoadModulesAsync(string directoryPath, CancellationToken ct = default)
  {
    if(!Directory.Exists(directoryPath)) return;

    // 1. Process and extract all .ares files
    var aresFiles = Directory.GetFiles(directoryPath, "*.ares");
    foreach(var file in aresFiles)
    {
      try
      {
        var dirName = Path.GetFileNameWithoutExtension(file);
        var targetDir = Path.Combine(directoryPath, dirName);

        if(Directory.Exists(targetDir))
        {
          Directory.Delete(targetDir, true);
        }

        ZipFile.ExtractToDirectory(file, targetDir);
        File.Delete(file); // Clean up the zip after extraction
      }
      catch(Exception)
      {
        // TODO: Log extraction error
      }
    }

    // 2. Load drivers from subdirectories containing a manifest
    var directories = Directory.GetDirectories(directoryPath);
    foreach(var dir in directories)
    {
      if(File.Exists(Path.Combine(dir, "manifest.yaml")))
      {
        try
        {
          var driver = await LoadFromDirectoryAsync(dir, ct);
          _driverRepo.AddOrUpdate(driver);
        }
        catch(Exception)
        {
          // TODO: Log load error
        }
      }
    }
  }

  public async Task<DeviceDriver> LoadFromDirectoryAsync(string moduleDirectory, CancellationToken ct = default)
  {
    var manifestPath = Path.Combine(moduleDirectory, "manifest.yaml");
    if(!File.Exists(manifestPath))
      throw new FileNotFoundException("Manifest file not found", manifestPath);

    var manifestYaml = await File.ReadAllTextAsync(manifestPath, ct);
    var manifest = _deserializer.Deserialize<DeviceDriverManifest>(manifestYaml);

    var assemblyPath = Path.Combine(moduleDirectory, "bin", manifest.AssemblyName);
    if(!File.Exists(assemblyPath))
      throw new FileNotFoundException($"Driver assembly not found: {assemblyPath}");

    var fileInfo = new FileInfo(assemblyPath);
    var loadContext = new AresDriverLoadContext(assemblyPath);
    Assembly assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

    var driverType = assembly.GetTypes().FirstOrDefault(t =>
        !t.IsInterface &&
        !t.IsAbstract &&
        t.GetInterfaces().Any(i => i.FullName == typeof(IAresDevice).FullName));

    if(driverType == null)
      throw new InvalidOperationException($"No IAresDevice implementation found in {manifest.AssemblyName}");

    Type? viewModelType = null;
    if(!string.IsNullOrEmpty(manifest.ViewModelTypeName))
    {
      viewModelType = assembly.GetType(manifest.ViewModelTypeName);
    }

    //Attempt to Manually Find the View Model
    else
    {
      viewModelType = assembly.GetTypes().FirstOrDefault(t =>
      !t.IsInterface &&
      !t.IsAbstract &&
      t.GetInterfaces().Any(i => i.FullName == typeof(IDeviceUnitControlViewModel).FullName));
    }

    var hashId = await ComputeFileHashAsync(assemblyPath, ct);

    return new DeviceDriver(hashId)
    {
      Manifest = manifest,
      Assembly = assembly,
      DriverType = driverType,
      ViewModelType = viewModelType,
      ModulePath = moduleDirectory,
      DriverSize = (int)fileInfo.Length
    };
  }

  public async Task<DeviceDriver> LoadAsync(string aresFilePath, CancellationToken ct = default)
  {
    var tempPath = Path.Combine(Path.GetTempPath(), "AresDevices", Path.GetFileNameWithoutExtension(aresFilePath));
    if(Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
    Directory.CreateDirectory(tempPath);

    ZipFile.ExtractToDirectory(aresFilePath, tempPath);
    return await LoadFromDirectoryAsync(tempPath, ct);
  }

  private async Task<string> ComputeFileHashAsync(string filePath, CancellationToken ct)
  {
    using var stream = File.OpenRead(filePath);
    var hashBytes = await SHA256.HashDataAsync(stream, ct);
    return Convert.ToHexString(hashBytes).ToLowerInvariant();
  }
}
