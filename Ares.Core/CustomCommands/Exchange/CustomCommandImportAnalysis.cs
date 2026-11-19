using Ares.Datamodel.Automation;

namespace Ares.Core.CustomCommands.Exchange;

public sealed class CustomCommandImportAnalysis
{
  public required CustomCommandVersion Definition { get; init; }
  public required IReadOnlyList<CustomCommandImportMatch> ExistingMatches { get; init; }
  public IReadOnlyList<CustomCommandDeviceReference> DeviceReferences { get; set; } = [];
  public CustomCommandVersion OriginalDefinition { get; init; } = new();
  public List<string> Warnings { get; init; } = [];
}