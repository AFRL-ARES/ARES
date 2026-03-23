using Ares.Datamodel.Analyzing;
using Ares.Services;
using Ares.Core.Grpc.Services;
using ReactiveUI;

namespace UI.Features.Analyzing.Settings;

public class AnalyzerConfigEditViewModel : ReactiveObject
{
  private readonly AnalyzerService _client;
  private readonly AnalyzerConfig _analyzerConfig;

  public AnalyzerConfigEditViewModel(AnalyzerService client)
  {
    _client = client;
    _analyzerConfig = new AnalyzerConfig();
    NewConfig = true;
  }

  public AnalyzerConfigEditViewModel(AnalyzerService client, AnalyzerConfig analyzerConfig)
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
