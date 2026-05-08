using Ares.Core.Device.Providers;
using Ares.Datamodel.Device;
using Ares.Datamodel.Templates;

namespace Ares.Core.Validation.Campaign;

internal class RequiredDeviceInterpretersValidator : ICampaignValidator
{
  private readonly IAresDeviceProvider _deviceProvider;
  public RequiredDeviceInterpretersValidator(IAresDeviceProvider deviceProvider)
  {
    _deviceProvider = deviceProvider;
  }

  public Task<ValidationResult> Validate(CampaignTemplate template)
  {
    var requiredDeviceIds = template.ExperimentTemplate.StepTemplates.SelectMany(stepTemp =>
        stepTemp.CommandTemplates.Select(cmdTemp => cmdTemp.Metadata.DeviceId)).Distinct().ToArray();

    var existingRequiredDevices = requiredDeviceIds
      .Select(_deviceProvider.GetDevice)
      .ToArray();

    var missingDeviceIds = requiredDeviceIds
      .Except(existingRequiredDevices.Select(device => device?.UniqueId)).ToArray();

    var offlineDevices = existingRequiredDevices
      .Where(device => device?.Status.OperationalState != OperationalState.Active).ToArray();

    var success = !missingDeviceIds.Any() && !offlineDevices.Any();
    var errorMessages = new List<string>();
    if(!success)
    {
      errorMessages.AddRange(missingDeviceIds.Select(deviceId => $"Device with Id {deviceId} is not present in the core"));
      errorMessages.AddRange(offlineDevices.Select(device => $"Device {device?.Name ?? "UNKNOWN DEVICE"} is not active"));
    }

    var validationResult = new ValidationResult(success, errorMessages);
    return Task.FromResult(validationResult);
  }

}
