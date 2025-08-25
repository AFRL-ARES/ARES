using Ares.Datamodel.Device;
using ReactiveUI;
using System;
using System.Reactive.Linq;

namespace UI.Backend.ViewModels.Settings.Device.Remote;

public class RemoteDeviceConfigEditViewModel : ReactiveObject
{
    private readonly RemoteDeviceConfig _originalConfig;
    private readonly ObservableAsPropertyHelper<bool> _modified;

    private string? _name;
    private int _port;
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
        if (Uri.TryCreate(_originalConfig.Url, UriKind.Absolute, out var uri))
        {
            _address = uri.Host;
            _port = uri.Port;
        }
        else
        {
            // Provide sensible defaults for new or malformed configurations
            _address = "localhost";
            _port = 5000;
        }

        // A reactive property that tracks if the view model has been modified.
        _modified = this.WhenAnyValue(
                x => x.Name,
                x => x.Address,
                x => x.Port)
            .Select(_ => Name != _originalConfig.Name || BuildUrl() != (_originalConfig.Url ?? string.Empty))
            .ToProperty(this, x => x.Modified, initialValue: false);
    }

    public string? Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public int Port
    {
        get => _port;
        set => this.RaiseAndSetIfChanged(ref _port, value);
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
        // Use UriBuilder to safely construct the URL.
        // Assuming http scheme. If https is needed, this could be another property.
        var builder = new UriBuilder("http", Address, Port);
        // ToString() on UriBuilder can add a trailing slash for default ports (e.g. http://host:80/), remove it for consistency.
        return builder.Uri.ToString().TrimEnd('/');
    }
}
