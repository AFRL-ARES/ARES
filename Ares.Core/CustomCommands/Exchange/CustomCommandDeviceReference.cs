using Ares.Core.CustomCommands.Exchange;

namespace Ares.Core.CustomCommands.Import;

public sealed class CustomCommandDeviceReference
{
  public required string DeviceIdentifier { get; set; }
  public string? CommandIdentifier { get; init; }
  public IReadOnlyList<CustomCommandDeviceReplacement> ReplacementDevices { get; set; } = [];
  public string? ResolvedDeviceId { get; set; }
  public string? ResolvedCommandName { get; set; }
  public string? SelectedReplacementDeviceId { get; set; }
  public string? SelectedReplacementDeviceName { get; set; }
  public CustomCommandDeviceReferenceStatus Status { get; set; } = CustomCommandDeviceReferenceStatus.Dynamic;
  public string Description { get; set; } = string.Empty;
  public int Line { get; init; }
  public int Column { get; init; }
}