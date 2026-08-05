using Ares.Core.Notifications;
using Ares.Core.Settings;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using System.Text.RegularExpressions;

namespace Ares.Core.Execution.VersionChecking;

public class DatamodelVersionValidator : IDatamodelVersionValidator
{
  private readonly INotificationHandler _notificationHandler;
  private readonly ISystemSettingsManager _settingsManager;
  private readonly ILogger<DatamodelVersionValidator> _logger;
  private static readonly Regex Pep440ToSemVerRegex = new(@"^(\d+\.\d+\.\d+)([a-zA-Z]+[\w\.-]*)$", RegexOptions.Compiled);

  public DatamodelVersionValidator(INotificationHandler notificationHandler, ISystemSettingsManager settingsManager, ILogger<DatamodelVersionValidator> logger)
  {
    _notificationHandler = notificationHandler;
    _settingsManager = settingsManager;
    _logger = logger;
  }

  public async Task<bool> CheckDatamodelVersionValidity(Metadata grpcMetadata, string externalServiceName)
  {
    var externalVersion = grpcMetadata.GetValue("datamodel-version");

    //If the version of the service is older than the update including headers simply ignore it for now
    if(externalVersion == null)
      return true;

    var normalizedExternalVersion = NormalizePep440ToSemVer(externalVersion);
    var parsed = SemanticVersion.TryParse(normalizedExternalVersion, out var externalSemanticVersion);

    if(!parsed || externalSemanticVersion is null)
    {
      await _notificationHandler.HandleNotification($"Datamodel Version Parsing Failed for {externalServiceName}",
        $"ARES failed to parse the datamodel version of your external service {externalServiceName}, this prevents ARES from safely checking compatibility with this service.",
        NotificationSeverityEnum.Warning, true);

      _logger.LogWarning($"Datamodel Version Parsing Failed for {externalServiceName}: {externalVersion}");
      return false;
    }

    var generalSettings = await _settingsManager.GetAresGeneralSettings();
    var displayWarnings = generalSettings?.DisplayCompatabilityWarnings ?? false;

    //Check for major version mismatch
    if(externalSemanticVersion.Major != MinimumRequiredDatamodelVersion.Major)
    {
      var majorMismatchMessage = $"ARES received a datamodel version of {externalVersion} from your service {externalServiceName}. " +
          $"This is incompatible with the major version of ARES Core, which requires {MinimumRequiredDatamodelVersion.Major}. " +
          "This can be fixed by updating your external service, such as by ensuring you are using the most up to date version of PyAres. ARES will continue to allow you to experiment, " +
          "but you may encounter unexpected failures when communicating with this service.";

      if(displayWarnings)
        await _notificationHandler.HandleNotification($"Incompatability Detected for {externalServiceName}", majorMismatchMessage, NotificationSeverityEnum.Error, true);

      _logger.LogWarning(majorMismatchMessage);
      return false;
    }

    //Check for minor version mismatch
    else if(externalSemanticVersion < MinimumRequiredDatamodelVersion)
    {
      var minorVersionMismatchMessage = $"ARES received a datamodel version of {externalVersion} from your service {externalServiceName}. " +
          $"This is less than the minimum required version of {MinimumRequiredDatamodelVersion}, meaning there may be incompatabilities between your ARES Core and this service. " +
          "This can be fixed by updating your external service, such as by ensuring you are using the most up to date version of PyAres. ARES will continue to allow you to experiment, " +
          "but you may encounter unexpected failures when communicating with this service.";

      if(displayWarnings)
        await _notificationHandler.HandleNotification($"Incompatability Detected for {externalServiceName}", minorVersionMismatchMessage, NotificationSeverityEnum.Warning, true);

      _logger.LogWarning(minorVersionMismatchMessage);
      return false;
    }

    return true;
  }

  public static string NormalizePep440ToSemVer(string rawVersion)
  {
    if(string.IsNullOrWhiteSpace(rawVersion))
      return rawVersion;

    return Pep440ToSemVerRegex.Replace(rawVersion.Trim(), "$1-$2");
  }

  public static SemanticVersion MinimumRequiredDatamodelVersion { get; set; } = SemanticVersion.Parse("0.33.0");
}
