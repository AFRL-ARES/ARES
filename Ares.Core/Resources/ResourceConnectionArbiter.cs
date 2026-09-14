using Ares.Core.Device.Providers;
using Ares.Device;
using System.Collections.Concurrent;

namespace Ares.Core.Resources;

/// <summary>
/// Manages access to shared resources to prevent conflicts.
/// </summary>
public class ResourceConnectionArbiter : IResourceConnectionArbiter
{
  private readonly ConcurrentDictionary<ConnectionResource, List<IAresDevice>> _resourceLocks = new ConcurrentDictionary<ConnectionResource, List<IAresDevice>>();
  private readonly IDeviceConfigProvider _deviceConfigProvider;

  public ResourceConnectionArbiter(IDeviceConfigProvider deviceConfigProvider)
  {
    _deviceConfigProvider = deviceConfigProvider;
  }

  /// <inheritdoc />
  public bool TryAcquireResource(ConnectionResource resource, IAresDevice requester)
  {
    if(string.IsNullOrEmpty(requester?.UniqueId) || resource == null)
      return false;

    if(!IsResourceInUse(resource))
      return _resourceLocks.TryAdd(resource, [requester]);

    else
    {
      var resourceOwners = GetResourceOwners(resource);

      if(resourceOwners is null || resourceOwners.Count == 0)
        return _resourceLocks.TryAdd(resource, [requester]);

      var resourceOwnerConfig = _deviceConfigProvider.GetConfigByDeviceId(resourceOwners.First().UniqueId);
      var requesterConfig = _deviceConfigProvider.GetConfigByDeviceId(requester.UniqueId);

      if(resourceOwnerConfig is null || requesterConfig is null)
        return false;

      //Dedicated is a specialized key term that signifies the connection cannot be shared with ANY other device
      if(resourceOwnerConfig.SerialInfo.Protocol == "Dedicated")
        return false;

      // If the device shares the same protocol, allow the resource allocation to pass.
      if(resourceOwnerConfig.SerialInfo.Protocol == requesterConfig.SerialInfo.Protocol)
        return true;

      return false;
    }
  }

  /// <inheritdoc />
  public void ReleaseResource(ConnectionResource resource, IAresDevice owner)
  {
    if(resource == null || owner == null)
        return;

    var ownerList = GetResourceOwners(resource);

    if(ownerList.Count == 0 || ownerList.Count == 1)
      _resourceLocks.TryRemove(new KeyValuePair<ConnectionResource, List<IAresDevice>>(resource, ownerList));


    var updatedOwnerList = ownerList.ToList();
    updatedOwnerList.Remove(owner);
    _resourceLocks.TryUpdate(resource, updatedOwnerList, ownerList);
  }

  /// <inheritdoc />
  public bool IsResourceInUse(ConnectionResource resource)
  {
    if(resource == null) 
      return false;
    
    return _resourceLocks.ContainsKey(resource);
  }

  /// <inheritdoc />
  public List<IAresDevice> GetResourceOwners(ConnectionResource resource)
  {
    if(resource == null) 
      return [];
    
    _resourceLocks.TryGetValue(resource, out var owner);
    return owner ?? [];
  }
}
