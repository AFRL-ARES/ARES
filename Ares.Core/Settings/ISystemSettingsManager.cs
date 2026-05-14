using Ares.Datamodel;

namespace Ares.Core.Settings;

public interface ISystemSettingsManager
{
  public Task Initialize();
  public Task UpdateErrorHandlingSettings(List<DeviceErrorHandlingConfig> configs);
  public Task<IEnumerable<DeviceErrorHandlingConfig>> GetCurrentErrorHandlingSettings();
  public Task<ErrorHandling> GetErrorHandlingByStatusCode(CommandStatusCode code);
  public Task<AresGeneralSettingsConfig?> GetAresGeneralSettings();
  public Task UpdateAresGeneralSettings(AresGeneralSettingsConfig config);
}
