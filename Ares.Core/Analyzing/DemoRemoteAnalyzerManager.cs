using Ares.Core.Execution.VersionChecking;
using Ares.Core.Notifications;
using Ares.Datamodel.Analyzing;

namespace Ares.Core.Analyzing;

/// <summary>
/// Demo-mode implementation of <see cref=\"IRemoteAnalyzerManager\"/>.
/// This manager does not read from or write to the database and operates
/// entirely against the in-memory analyzer repository.
/// </summary>
public class DemoRemoteAnalyzerManager : IRemoteAnalyzerManager
{
  private readonly IAnalyzerRepo _analyzerRepo;
  private readonly INotificationHandler _notificationHandler;
  private readonly IDatamodelVersionValidator _datamodelVersionValidator;

  private static readonly string _demoAnalyzerUniqueId = "9e5a8f3b-5c7d-4a1b-9f0a-1a2b3c4d5e6f";

  public DemoRemoteAnalyzerManager(
    IAnalyzerRepo analyzerRepo,
    INotificationHandler notificationHandler,
    IDatamodelVersionValidator datamodelVersionValidator)
  {
    _analyzerRepo = analyzerRepo;
    _notificationHandler = notificationHandler;
    _datamodelVersionValidator = datamodelVersionValidator;
  }

  public async Task LoadAnalyzers()
  {
    // In demo mode we only ensure that the static demo analyzer exists.
    var existingDemo = _analyzerRepo.GetAnalyzerById(_demoAnalyzerUniqueId);
    if(existingDemo is null)
    {
      // Default demo analyzer endpoint from DemoRemoteAnalyzer launch settings.
      var demoUrl = "http://localhost:5026";
      await CreateDemoAnalyzer(demoUrl);
    }
  }

  public Task CreateAnalyzer(string name, string url)
  {
    // In demo mode, creating additional analyzers is allowed but purely in-memory.
    var config = new AnalyzerConfig { UniqueId = Guid.NewGuid().ToString(), Name = name, Url = url };
    var analyzer = ConfigToAnalyzer(config);

    if(analyzer is not null)
    {
      _analyzerRepo.AddAnalyzer(analyzer);
    }

    return Task.CompletedTask;
  }

  public Task CreateDemoAnalyzer(string url)
  {
    var config = new AnalyzerConfig { UniqueId = _demoAnalyzerUniqueId, Name = "Demo Remote Analyzer", Url = url };
    var analyzer = ConfigToAnalyzer(config);

    if(analyzer is not null)
    {
      _analyzerRepo.AddAnalyzer(analyzer);
    }

    return Task.CompletedTask;
  }

  public Task RemoveAnalyzer(string analyzerId)
  {
    _analyzerRepo.RemoveAnalyzer(analyzerId);
    return Task.CompletedTask;
  }

  public Task UpdateAnalyzer(AnalyzerConfig config)
  {
    // For demo mode we can implement a simple in-memory update by
    // recreating the analyzer with the new configuration.
    var existing = _analyzerRepo.GetAnalyzerById(config.UniqueId);
    if(existing is not null)
    {
      _analyzerRepo.RemoveAnalyzer(config.UniqueId);
      var updated = ConfigToAnalyzer(config);
      if(updated is not null)
      {
        _analyzerRepo.AddAnalyzer(updated);
      }
    }

    return Task.CompletedTask;
  }

  public Task UpdateAnalyzerSettings(AnalyzerSettings analyzerSettings)
  {
    var analyzer = _analyzerRepo.GetAnalyzerById(analyzerSettings.AnalyzerId);
    if(analyzer is null)
    {
      return Task.CompletedTask;
    }

    analyzer.UpdateSettings(analyzerSettings.Settings);
    // No persistence in demo mode.
    return Task.CompletedTask;
  }

  private RemoteAnalyzer? ConfigToAnalyzer(AnalyzerConfig config)
  {
    var uriValid = Uri.TryCreate(config.Url, UriKind.Absolute, out var uri);
    if(!uriValid || uri is null)
    {
      _ = _notificationHandler.HandleNotification(
        "Analyzer Load Error",
        $"Failed to load a remote analyzer {config.Name} because the url {config.Url} is invalid.",
        NotificationSeverityEnum.Danger);
      return null;
    }

    return new RemoteAnalyzer(config.Name, uri, _datamodelVersionValidator, config.UniqueId);
  }
}
