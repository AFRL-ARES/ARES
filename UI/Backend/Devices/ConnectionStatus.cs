namespace UI.Backend.Devices;

public enum ConnectionStatus
{
  Undefined,
  Disconnected,
  // we've connected to the service and are receiving updates
  // but the service does not have connection to the device itself
  ConnectedToService,
  // the UI is talking to service and service is talking to the device
  ConnectedToDevice,
}
