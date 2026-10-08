using Ares.Core.Grpc.Services;
using Ares.Datamodel.Visualizing;
using ReactiveUI;

namespace UI.Features.Visualizing.Settings;

public class VisualizerConfigEditViewModel : ReactiveObject
{
  private readonly VisualizerConfig _visualizerConfig;

  public VisualizerConfigEditViewModel(VisualizerService client)
  {
    _visualizerConfig = new VisualizerConfig();
    NewConfig = true;
  }

  public VisualizerConfigEditViewModel(VisualizerService client, VisualizerConfig visualizerConfig)
  {
    _visualizerConfig = visualizerConfig;
    Name = visualizerConfig.Name;
    Address = visualizerConfig.Url;
  }

  public string? Name { get; set; }

  public string Address { get; set; } = "http://localhost";

  public bool Modified => _visualizerConfig.Name != Name || _visualizerConfig.Url != Address;

  public bool NewConfig { get; set; }

  public VisualizerConfig Save()
    => Modified ? new VisualizerConfig { Name = Name, Url = Address, UniqueId = _visualizerConfig.UniqueId } : _visualizerConfig;
}
