using Ares.Core.Device;
using Ares.Device;
using Ares.Datamodel.Device;
using Ares.Datamodel.Templates;

namespace Ares.Core.Validation.Campaign;

internal class RequiredDeviceInterpretersValidator : ICampaignValidator
{
  private readonly IDeviceCommandInterpreterRepo _deviceCommandInterpreterRepo;
  public RequiredDeviceInterpretersValidator(IDeviceCommandInterpreterRepo deviceCommandInterpreterRepo)
  {
    _deviceCommandInterpreterRepo = deviceCommandInterpreterRepo;
  }

  public Task<ValidationResult> Validate(CampaignTemplate template)
  {
    var requiredDeviceIds = template.ExperimentTemplate.StepTemplates.SelectMany(stepTemp =>
        stepTemp.CommandTemplates.Select(cmdTemp => cmdTemp.Metadata.DeviceId)).Distinct().ToArray();

    var availableInterpreters = requiredDeviceIds.Select(deviceName =>
        _deviceCommandInterpreterRepo
        .FirstOrDefault(interpreter => interpreter.Device.Name.Equals(deviceName)))
      .OfType<IDeviceCommandInterpreter<IAresDevice>>()
      .ToArray();

    var missingDeviceNames = requiredDeviceIds
      .Except(availableInterpreters.Select(interpreter => interpreter.Device.Name)).ToArray();

    var invalidAvailableInterpreters = availableInterpreters
      .Where(interpreter => interpreter.Device.Status.OperationalState != OperationalState.Active).ToArray();

    var success = !missingDeviceNames.Any() && !invalidAvailableInterpreters.Any();
    var errorMessages = new List<string>();
    if(!success)
    {
      errorMessages.AddRange(missingDeviceNames.Select(deviceName => $"{deviceName} is not detected in the core"));
      errorMessages.AddRange(invalidAvailableInterpreters.Select(interpreter => $"{interpreter.Device.Name} is not active"));
    }

    var validAtionResult = new ValidationResult(success, errorMessages);
    return Task.FromResult(validAtionResult);
  }

}
