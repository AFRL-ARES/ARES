using Ares.Device;
using System.Collections.Concurrent;

namespace Ares.Core.Resources;

/// <summary>
/// Manages access to shared resources to prevent conflicts.
/// </summary>
public class ResourceConnectionArbiter : IResourceConnectionArbiter
{
  private readonly ConcurrentDictionary<ConnectionResource, IAresDevice> _resourceLocks = new ConcurrentDictionary<ConnectionResource, IAresDevice>();

  /// <inheritdoc />
  public bool TryAcquireResource(ConnectionResource resource, IAresDevice owner)
  {
      if(string.IsNullOrEmpty(owner?.UniqueId) || resource == null)
          return false;

      return _resourceLocks.TryAdd(resource, owner);
  }

  /// <inheritdoc />
  public void ReleaseResource(ConnectionResource resource, IAresDevice owner)
  {
      if(resource == null || owner == null)
          return;
          
      _resourceLocks.TryRemove(new KeyValuePair<ConnectionResource, IAresDevice>(resource, owner));
  }

  /// <inheritdoc />
  public bool IsResourceInUse(ConnectionResource resource)
  {
      if(resource == null) return false;
      return _resourceLocks.ContainsKey(resource);
  }

  /// <inheritdoc />
  public IAresDevice? GetResourceOwner(ConnectionResource resource)
  {
      if(resource == null) return null;
      _resourceLocks.TryGetValue(resource, out var owner);
      return owner;
  }
}
