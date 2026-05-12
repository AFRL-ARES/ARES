using Ares.Datamodel;

namespace Ares.Core.Settings;

public interface ISystemSettingsManager
{
  public Task UpdateErrorHandlingSettings(List<DeviceErrorHandlingConfig> configs);

  public Task<IEnumerable<DeviceErrorHandlingConfig>> GetCurrentErrorHandlingSettings();
}
