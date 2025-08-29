using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Ares.Datamodel.Device;

namespace Ares.Device;

public abstract class AresDevice : IAresDevice
{
  private readonly BehaviorSubject<DeviceOperationalStatus> _statusSubject;
  private readonly ISubject<DeviceOperationalStatus> _statusSink;
  private readonly object _statusGate = new();
  private DeviceOperationalStatus _status;
  private bool _disposed;

  protected AresDevice(string name)
    : this(name, Guid.NewGuid().ToString()) { }

  protected AresDevice(string name, string id)
  {
    if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Device name is required.", nameof(name));
    if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("UniqueId is required.", nameof(id));

    Name = name;
    UniqueId = id;

    _status = new DeviceOperationalStatus
    {
      OperationalState = OperationalState.Inactive,
      Message = $"{Name} constructed. Activation has not been called yet."
    };

    _statusSubject = new BehaviorSubject<DeviceOperationalStatus>(_status);
    _statusSink = Subject.Synchronize(_statusSubject);
  }

  public string Name { get; }

  public DeviceOperationalStatus Status
  {
    get => _status;
    protected set
    {
      ArgumentNullException.ThrowIfNull(value);

      bool shouldEmit;

      lock (_statusGate)
      {
        if (_disposed) return; // ignore updates after disposal
        if (ReferenceEquals(value, _status)) return; // suppress redundant reassignments of same instance

        _status = value;
        shouldEmit = true;
      }

      if (shouldEmit)
      {
        _statusSink.OnNext(value); // emit outside lock, serialized to preserve call order
      }
    }
  }

  public IObservable<DeviceOperationalStatus> StatusObservable => _statusSubject.AsObservable();

  public string Version { get; protected set; } = "0.0.0";
  public string Type { get; protected set; } = "";
  public string Description { get; protected set; } = "";
  public string UniqueId { get; init; }

  public abstract Task<bool> Activate(CancellationToken ct);
  public abstract Task EnterSafeMode(CancellationToken ct);

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing)
  {
    if (_disposed) return;

    if (!disposing) return;
    
    lock (_statusGate)
    {
      if (_disposed) return;
      _disposed = true; // mark disposed under the gate to block further updates
    }

    // Complete and dispose after releasing the gate to avoid notifying under locks
    _statusSubject.OnCompleted();
    _statusSubject.Dispose();
  }
}
