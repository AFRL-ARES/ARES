using Ares.Datamodel;
using Ares.Datamodel.Device;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Ares.Device;

public abstract class AresDevice : IAresDevice
{
  private readonly BehaviorSubject<DeviceOperationalStatus> _statusSubject;
  private readonly ISubject<DeviceOperationalStatus> _statusSink;
  private readonly object _statusGate = new();
  private DeviceOperationalStatus _status;
  private bool _disposed;

  protected AresDevice(string name, string id)
  {
    Name = name;
    UniqueId = id;

    // Initialize status logic
    _status = new DeviceOperationalStatus { OperationalState = OperationalState.Inactive };
    _statusSubject = new BehaviorSubject<DeviceOperationalStatus>(_status);
    _statusSink = Subject.Synchronize(_statusSubject);
    Status = new DeviceOperationalStatus();
  }

  protected AresDevice(string name) : this(name, Guid.NewGuid().ToString()) 
  { 
  
  }

  #region Metadata & Discovery (New)

  public virtual IReadOnlyList<DeviceCommandDescriptor> CommandDescriptors { get; protected set; } = Array.Empty<DeviceCommandDescriptor>();
  public virtual AresDataSchema StateSchema { get; protected set; } = new();
  public virtual AresDataSchema SettingSchema { get; protected set; } = new();

  #endregion

  #region Generic Interaction
  public abstract Task<CommandResult> ExecuteCommand(string command, AresStruct arguments, CancellationToken token);

  public abstract Task UpdateSettings(AresStruct settings);

  #endregion

  #region Existing Contract

  public string Name { get; }
  public string UniqueId { get; init; }
  public string Version { get; protected set; } = "0.0.0";
  public string Type { get; protected set; } = "";
  public string Description { get; protected set; } = "";
  public string HardwareIdentity { get; protected set; } = "";

  public DeviceOperationalStatus Status { get; protected set; }

  public IObservable<DeviceOperationalStatus> StatusObservable => _statusSubject.AsObservable();
  public abstract IObservable<AresStruct> StateStream { get; }

  public abstract Task<bool> Activate(CancellationToken ct);
  public abstract Task EnterSafeMode(CancellationToken ct);
  public abstract Task<AresStruct> GetState();

  #endregion

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
