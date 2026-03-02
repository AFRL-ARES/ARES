using Ares.Core.Device.Repos;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Ares.Core.Device.Loaders;

public class DeviceDriverLoader : IDeviceDriverLoader
{
  private readonly IDeviceDriverRepo _driverRepo;
  private readonly IDeserializer _deserializer;

  public DeviceDriverLoader(IDeviceDriverRepo driverRepo)
  {
    _driverRepo = driverRepo;
    _deserializer = new DeserializerBuilder()
      .WithNamingConvention(CamelCaseNamingConvention.Instance)
      .IgnoreUnmatchedProperties()
      .Build();
  }

  public async Task LoadModulesAsync(string directoryPath, CancellationToken ct = default)
  {
    if(!Directory.Exists(directoryPath))
    {
      return;
    }

    // Process all .ares files first
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
        File.Delete(file);
      }
      catch(Exception)
      {
        // Log error unzipping specific file
      }
    }

    // Load from all subdirectories that contain a manifest.yaml
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
          // Log error loading specific module
        }
      }
    }
  }

  public async Task<DeviceDriver> LoadAsync(string aresFilePath, CancellationToken ct = default)
  {
    if(!File.Exists(aresFilePath))
      throw new FileNotFoundException("Device module file not found", aresFilePath);

    var tempPath = Path.Combine(Path.GetTempPath(), "AresDevices", Path.GetFileNameWithoutExtension(aresFilePath));
    if(Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
    Directory.CreateDirectory(tempPath);

    ZipFile.ExtractToDirectory(aresFilePath, tempPath);
    
    return await LoadFromDirectoryAsync(tempPath, ct);
  }

  public async Task<DeviceDriver> LoadFromDirectoryAsync(string moduleDirectory, CancellationToken ct = default)
  {
    if(!Directory.Exists(moduleDirectory))
      throw new DirectoryNotFoundException($"Module directory not found: {moduleDirectory}");

    var manifestPath = Path.Combine(moduleDirectory, "manifest.yaml");
    if(!File.Exists(manifestPath))
      throw new FileNotFoundException("Manifest file not found in device module", manifestPath);

    var manifestYaml = await File.ReadAllTextAsync(manifestPath, ct);
    var manifest = _deserializer.Deserialize<DeviceManifest>(manifestYaml);

    var dllFiles = Directory.GetFiles(moduleDirectory, "*.dll");
    Assembly? mainAssembly = null;
    Type? driverType = null;
    Type? viewModelType = null;

    foreach(var dll in dllFiles)
    {
      try
      {
        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);
        
        if(manifest.DriverClass != null && driverType == null)
        {
          driverType = assembly.GetType(manifest.DriverClass);
          if(driverType != null) mainAssembly = assembly;
        }

        if(manifest.ViewModelClass != null && viewModelType == null)
        {
          viewModelType = assembly.GetType(manifest.ViewModelClass);
        }
      }
      catch(Exception)
      {
        // Log or ignore if DLL cannot be loaded
      }
    }

    if(driverType == null)
      throw new InvalidOperationException($"Could not find driver class '{manifest.DriverClass}' in any assembly within the module.");

    return new DeviceDriver
    {
      Manifest = manifest,
      Assembly = mainAssembly!,
      DriverType = driverType,
      ViewModelType = viewModelType,
      ModulePath = moduleDirectory
    };
  }
}
