namespace UI.Backend.ViewModels.DeviceStateLogging;

public interface ICombinedDeviceStateIdGetter
{
  Task<IEnumerable<string>> GetAvailableIds();
}
