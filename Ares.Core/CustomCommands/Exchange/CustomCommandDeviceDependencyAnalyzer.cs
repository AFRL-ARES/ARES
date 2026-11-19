using Ares.Core.Device.Providers;
using Ares.Core.Scripting;
using Ares.Datamodel;
using Ares.Datamodel.Automation;
using Ares.Datamodel.Device;
using AresScript;
using AresScript.Generated;
using AresScript.ScriptBuilding;
using AresScript.Symbols;
using Antlr4.Runtime;
using Microsoft.Extensions.Logging;

namespace Ares.Core.CustomCommands.Exchange;

public sealed class CustomCommandDeviceDependencyAnalyzer(IAresDeviceProvider _deviceProvider, ILogger<CustomCommandDeviceDependencyAnalyzer> _logger)
{
  public async Task<IReadOnlyList<CustomCommandDeviceReference>> AnalyzeAsync(CustomCommandVersion command, CancellationToken cancellationToken = default)
  {
    var wrappedScript = CustomCommandScriptBuilder.BuildWrappedScript(
      command.Name,
      command.InputParameters.Select(parameter => new AresScriptParameter(parameter.Name, parameter.Schema ?? new AresValueSchema())),
      command.OutputSchema,
      command.ScriptBody);

    var references = new List<CustomCommandDeviceReference>();
    var lexer = new AresIndentationLexer(new AntlrInputStream(wrappedScript));
    var parser = new AresLangParser(new CommonTokenStream(lexer));
    new DeviceReferenceVisitor(references).Visit(parser.program());

    var devices = _deviceProvider.GetAllDevices();
    foreach(var reference in references)
    {
      cancellationToken.ThrowIfCancellationRequested();
      reference.ReplacementDevices = await FindReplacementDevicesAsync(reference, devices, cancellationToken);
      var device = devices.FirstOrDefault(candidate => string.Equals(
        ScriptIdentifierHelper.SanitizeIdentifier(string.IsNullOrWhiteSpace(candidate.Name) ? candidate.UniqueId : candidate.Name),
        reference.DeviceIdentifier,
        StringComparison.Ordinal));

      if(device is null)
      {
        reference.Status = CustomCommandDeviceReferenceStatus.DeviceMissing;
        reference.Description = $"Device '{reference.DeviceIdentifier}' was not found.";
        continue;
      }

      reference.ResolvedDeviceId = device.UniqueId;
      if(string.IsNullOrWhiteSpace(reference.CommandIdentifier))
      {
        reference.Status = CustomCommandDeviceReferenceStatus.Valid;
        reference.Description = $"Device '{device.Name}' found.";
        continue;
      }

      IReadOnlyList<DeviceCommandDescriptor> descriptors;
      try
      {
        descriptors = await device.GetCommandDescriptorsAsync();
      }
      catch(Exception exception)
      {
        reference.Status = CustomCommandDeviceReferenceStatus.Unverifiable;
        reference.Description = $"Device '{device.Name}' was found, but its command interface could not be loaded: {exception.Message}";
        continue;
      }

      var descriptor = descriptors.FirstOrDefault(candidate => string.Equals(ScriptIdentifierHelper.SanitizeIdentifier(candidate.Name),
        reference.CommandIdentifier,
        StringComparison.Ordinal));

      if(descriptor is null)
      {
        reference.Status = CustomCommandDeviceReferenceStatus.CommandMissing;
        reference.Description = $"Command '{reference.CommandIdentifier}' was not found on device '{device.Name}'.";
        continue;
      }

      reference.ResolvedCommandName = descriptor.Name;
      reference.Status = CustomCommandDeviceReferenceStatus.Valid;
      reference.Description = $"Device '{device.Name}' and command '{descriptor.Name}' found.";
    }

    return references;
  }

  private async Task<IReadOnlyList<CustomCommandDeviceReplacement>> FindReplacementDevicesAsync(CustomCommandDeviceReference reference, 
    IReadOnlyCollection<Ares.Device.IAresDevice> devices,
    CancellationToken cancellationToken)
  {
    var replacements = new List<CustomCommandDeviceReplacement>();
    foreach(var device in devices)
    {
      cancellationToken.ThrowIfCancellationRequested();
      if(string.IsNullOrWhiteSpace(reference.CommandIdentifier))
      {
        replacements.Add(new CustomCommandDeviceReplacement { DeviceId = device.UniqueId, Name = device.Name });
        continue;
      }

      try
      {
        var descriptors = await device.GetCommandDescriptorsAsync();
        if(descriptors.Any(descriptor => string.Equals(ScriptIdentifierHelper.SanitizeIdentifier(descriptor.Name), reference.CommandIdentifier, StringComparison.Ordinal)))
          replacements.Add(new CustomCommandDeviceReplacement { DeviceId = device.UniqueId, Name = device.Name });
      }

      catch(Exception ex)
      {
        _logger.LogError(ex, "Encountered an error trying to fetch device command descriptors for custom command replacement analysis");
      }
    }

    return replacements.OrderBy(replacement => replacement.Name, StringComparer.OrdinalIgnoreCase).ToArray();
  }
}