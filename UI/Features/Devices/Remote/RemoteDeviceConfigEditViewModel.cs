using System.Reactive.Linq;
using Ares.Datamodel.Device;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace UI.Features.Devices.Remote;

public partial class RemoteDeviceConfigEditViewModel : ReactiveObject
{
  private readonly RemoteDeviceConfig _originalConfig;
  private readonly ObservableAsPropertyHelper<bool> _modified;

  // Constructor for a new configuration
  public RemoteDeviceConfigEditViewModel() : this(new RemoteDeviceConfig(), isNew: true)
  {
  }

  // Constructor for editing an existing configuration
  public RemoteDeviceConfigEditViewModel(RemoteDeviceConfig remoteDeviceConfig) : this(remoteDeviceConfig, isNew: false)
  {
  }

  // Private master constructor to reduce duplication
  private RemoteDeviceConfigEditViewModel(RemoteDeviceConfig config, bool isNew)
  {
    _originalConfig = config ?? throw new ArgumentNullException(nameof(config));
    NewConfig = isNew;

    // Set initial values from the model
    if(_originalConfig.Name != string.Empty)
      Name = _originalConfig.Name;

    if(_originalConfig.Url != string.Empty)
      Address = _originalConfig.Url;

    // A reactive property that tracks if the view model has been modified.
    _modified = this.WhenAnyValue(x => x.Name, x => x.Address)
        .Select(_ => Name != _originalConfig.Name || BuildUrl() != (_originalConfig.Url ?? string.Empty))
        .ToProperty(this, x => x.Modified, initialValue: false);
  }

  [Reactive]
  public partial string Name { get; set; }

  [Reactive]
  public partial string Address { get; set; } = "http://localhost";

  public bool Modified => _modified.Value;

  public bool NewConfig { get; }

  public RemoteDeviceConfig Save()
      => Modified ? new RemoteDeviceConfig { Name = Name, Url = BuildUrl() } : _originalConfig;

  private string BuildUrl()
  {
    var success = Uri.TryCreate(Address, UriKind.Absolute, out var result);
    return result?.ToString().TrimEnd('/') ?? "";
  }
}
