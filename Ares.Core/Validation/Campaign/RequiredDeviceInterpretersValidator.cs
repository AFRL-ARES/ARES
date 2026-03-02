using Ares.Core.Device.Repos;
using Ares.Datamodel.Device;
using Ares.Datamodel.Templates;
using Ares.Device;

namespace Ares.Core.Validation.Campaign;

internal class RequiredDeviceInterpretersValidator : ICampaignValidator
{
  private readonly IAresDeviceRepo _deviceRepo;
  public RequiredDeviceInterpretersValidator(IAresDeviceRepo deviceRepo)
  {
    _deviceRepo = deviceRepo;
  }

  public Task<ValidationResult> Validate(CampaignTemplate template)
  {
    var requiredDeviceIds = template.ExperimentTemplate.StepTemplates.SelectMany(stepTemp =>
        stepTemp.CommandTemplates.Select(cmdTemp => cmdTemp.Metadata.DeviceId)).Distinct().ToArray();

    var existingRequiredDevices = requiredDeviceIds
      .Select(_deviceRepo.GetDevice)
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
      errorMessages.AddRange(offlineDevices.Select(device => $"Device {device.Name} is not active"));
    }

    var validationResult = new ValidationResult(success, errorMessages);
    return Task.FromResult(validationResult);
  }

}
