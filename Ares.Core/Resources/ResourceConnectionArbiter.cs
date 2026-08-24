using Ares.Device;
using System.Collections.Concurrent;

namespace Ares.Core.Resources;

/// <summary>
/// Manages access to shared resources to prevent conflicts.
/// </summary>
public class ResourceConnectionArbiter : IResourceConnectionArbiter
{
  private readonly ConcurrentDictionary<ConnectionResource, List<IAresDevice>> _resourceLocks = new ConcurrentDictionary<ConnectionResource, List<IAresDevice>>();

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

      if(resourceOwners is null)
        return _resourceLocks.TryAdd(resource, [requester]);

      var requestingDeviceType = requester.GetType();
      var resourceOwnerType = resourceOwners?.First().GetType();

      // If the device is of the exact same type, allow the resource allocation to pass.
      if(requestingDeviceType.Name == resourceOwnerType?.Name)
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
