using Ares.Messaging;

namespace Ares.Device;

public static class AresDeviceHelpers
{
  public static DeviceCommandResult ParseCommandParameterToInt(Parameter param, out int parsedParam)
  {
    var result = new DeviceCommandResult();
    var parsed = int.TryParse(param.Value.Value.StringValue, out var intParam);

    if(!parsed)
    {
      result.Error = $"Failed to parse {param.Metadata.Name} into integer!";
      result.Success = false;
      parsedParam = -1;
      return result;
    }

    else
    {
      parsedParam = intParam;
      result.Success = true;
    }

    return result;
  }

  public static DeviceCommandResult ParseStringCommandParameterToDouble(Parameter param, out double parsedParam)
  {
    var result = new DeviceCommandResult();
    var parsed = double.TryParse(param.Value.Value.StringValue, out var doubleParam);

    if(!parsed)
    {
      result.Error = $"Failed to parse {param.Metadata.Name} into double!";
      result.Success = false;
      parsedParam = -1;
      return result;
    }

    else
    {
      parsedParam = doubleParam;
      result.Success = true;
    }

    return result;
  }
}
