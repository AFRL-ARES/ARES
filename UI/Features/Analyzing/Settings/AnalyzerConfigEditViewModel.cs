using Ares.Datamodel.Analyzing;
using Ares.Services;
using ReactiveUI;

namespace UI.Features.Analyzing.Settings;

public class AnalyzerConfigEditViewModel : ReactiveObject
{
  private readonly AresAnalyzerManagementService.AresAnalyzerManagementServiceClient _client;
  private readonly AnalyzerConfig _analyzerConfig;

  public AnalyzerConfigEditViewModel(AresAnalyzerManagementService.AresAnalyzerManagementServiceClient client)
  {
    _client = client;
    _analyzerConfig = new AnalyzerConfig();
    NewConfig = true;
  }

  public AnalyzerConfigEditViewModel(AresAnalyzerManagementService.AresAnalyzerManagementServiceClient client, AnalyzerConfig analyzerConfig)
  {
    _client = client;
    _analyzerConfig = analyzerConfig;
    Name = _analyzerConfig.Name;
    Address = _analyzerConfig.Url;
  }

  public string? Name { get; set; }

  public int Port { get; set; }

  public string Address { get; set; } = "http://localhost";

  public bool Modified => _analyzerConfig.Name != Name || _analyzerConfig.Url != Address;

  public bool NewConfig { get; set; }

  public AnalyzerConfig Save()
    => Modified ? new AnalyzerConfig { Name = Name, Url = Address } : _analyzerConfig;
}
