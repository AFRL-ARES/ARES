using Ares.Core.Device.Remote;
using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Toolkit.Device.UI;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using UI.Application.Devices;

namespace UI.Features.Devices.Remote;

public partial class RemoteDeviceUnitViewModel : ReactiveObject, IDeviceUnitControlViewModel, IAsyncDisposable
{
  private const int MinimumWidth = 30;
  private const int WidthPerStateColumn = 15;
  private const int MaximumStateColumns = 4;
  private readonly IAresDeviceAdapter _deviceAdapter;
  private IDisposable? _stateListener;
  private IDisposable? _statusListener;

  public RemoteDeviceUnitViewModel(IAresDeviceAdapter deviceAdapter)
  {
    DeviceId = deviceAdapter.Id;
    _deviceAdapter = deviceAdapter;
    StateDisplayInfo = deviceAdapter.GetDisplaySettings();

    ViewType = typeof(RemoteDeviceUnitView);
    DefaultWidth = MinimumWidth;
    StateColumnCount = 1;

    StartStateUpdater();
  }

  private void StartStateUpdater()
  {
    _stateListener = _deviceAdapter.StateStream.Subscribe(s =>
    {
      DeviceState = s;
      StateDisplayInfo = _deviceAdapter.GetDisplaySettings();
      UpdateLayout(s.Fields.Count(f => ShouldDisplay(f.Key)));
    });
    _statusListener = _deviceAdapter.ConnectionStatusStream.Subscribe(s =>
    {
      ConnectionStatus = s;
      if(s == ConnectionStatus.ConnectedToDevice)
        _ = RefreshCommandsAsync();
    });
    _ = RefreshCommandsAsync();
  }

  public bool ShouldDisplay(string fieldName)
  {
    var found = StateDisplayInfo.TryGetValue(fieldName, out StateDisplayInfo? value);

    if(!found || value is null)
      return false;

    else
      return value.DisplayOnUI;
  }

  private async Task RefreshCommandsAsync()
  {
    try
    {
      DeviceCommands = (await _deviceAdapter.GetCommandDescriptorsAsync()).ToArray();
    }
    catch(Exception)
    {
      // Keep the last known command set available while the device reconnects.
    }
  }

  private void UpdateLayout(int stateFieldCount)
  {
    StateColumnCount = Math.Clamp((int)Math.Ceiling(stateFieldCount / 2d), 1, MaximumStateColumns);
    DefaultWidth = Math.Max(MinimumWidth, StateColumnCount * WidthPerStateColumn);
  }

  public ValueTask DisposeAsync()
  {
    _stateListener?.Dispose();
    _statusListener?.Dispose();
    GC.SuppressFinalize(this);
    return ValueTask.CompletedTask;
  }

  public string DeviceName => _deviceAdapter.Name;
  public IAresDeviceAdapter DeviceAdapter => _deviceAdapter;
  public string DeviceId { get; }
  [Reactive]
  public partial int DefaultWidth { get; set; }
  public Type? ViewType { get; set; }
  [Reactive]
  public partial AresStruct? DeviceState { get; set; }
  [Reactive]
  public partial Dictionary<string, StateDisplayInfo> StateDisplayInfo { get; set; }
  [Reactive]
  public partial ConnectionStatus ConnectionStatus { get; set; }
  [Reactive]
  public partial DeviceCommandDescriptor[] DeviceCommands { get; private set; } = [];
  [Reactive]
  public partial int StateColumnCount { get; private set; }
}