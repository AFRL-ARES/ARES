namespace UI.Backend.ViewModels.DeviceStateLogging;

public interface ICombinedDeviceIdGetter
{
  Task<IEnumerable<string>> GetAvailableIds();
}
