using System.Text.Json.Serialization;

namespace Ares.Core.Device.Plugins.Manifest;

/// <summary>
/// Represents the root manifest structure for an ARES Device Driver.
/// This matches the schema of the YAML manifest file.
/// </summary>
public class DeviceDriverManifest
{
  [JsonPropertyName("device_type_name")]
  public string DeviceTypeName { get; set; } = string.Empty;

  [JsonPropertyName("driver_id")]
  public string DriverId { get; set; } = string.Empty;

  /// <summary>
  /// The version of the driver (e.g., "1.0.0").
  /// </summary>
  [JsonPropertyName("version")]
  public string Version { get; set; } = "1.0.0";

  [JsonPropertyName("connection_type")]
  public string ConnectionType { get; set; } = string.Empty;

  /// <summary>
  /// The name of the DLL (e.g., "AlicatMFCRemastered.dll") that contains 
  /// the driver logic and ViewModels.
  /// </summary>
  [JsonPropertyName("assembly_name")]
  public string AssemblyName { get; set; } = string.Empty;

  /// <summary>
  /// The fully qualified name of the ViewModel class (e.g., "Alicat.UI.AlicatMFCViewModel").
  /// Used by the Factory to instantiate the correct UI representation.
  /// </summary>
  [JsonPropertyName("view_model_type_name")]
  public string ViewModelTypeName { get; set; } = string.Empty;

  /// <summary>
  /// A list of configuration settings required by the ARES UI to 
  /// successfully initialize the device.
  /// </summary>
  [JsonPropertyName("settings")]
  public List<DriverSettingDefinition> Settings { get; set; } = new();
}
