using System.Reactive.Linq;
using Ares.Datamodel.Device;
using ReactiveUI;

namespace UI.Features.Devices.Remote;

public class RemoteDeviceConfigEditViewModel : ReactiveObject
{
  private readonly RemoteDeviceConfig _originalConfig;
  private readonly ObservableAsPropertyHelper<bool> _modified;

  private string _name;
  private string _address;

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
    _name = _originalConfig.Name;
    _address = _originalConfig.Url;

    // A reactive property that tracks if the view model has been modified.
    _modified = this.WhenAnyValue(
            x => x.Name,
            x => x.Address)
        .Select(_ => Name != _originalConfig.Name || BuildUrl() != (_originalConfig.Url ?? string.Empty))
        .ToProperty(this, x => x.Modified, initialValue: false);
  }

  public string Name
  {
    get => _name;
    set => this.RaiseAndSetIfChanged(ref _name, value);
  }

  public string Address
  {
    get => _address;
    set => this.RaiseAndSetIfChanged(ref _address, value);
  }

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
