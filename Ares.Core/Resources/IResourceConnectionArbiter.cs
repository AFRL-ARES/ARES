using Ares.Device;

namespace Ares.Core.Resources;

/// <summary>
/// Provides a mechanism for arbitrating access to shared resources, such as serial ports.
/// </summary>
public interface IResourceConnectionArbiter
{
  /// <summary>
  /// Attempts to acquire a lock on a resource for a specific owner.
  /// </summary>
  /// <param name="resource">The resource to acquire.</param>
  /// <param name="owner">The owner requesting the resource.</param>
  /// <returns>True if the resource was successfully acquired, false otherwise.</returns>
  bool TryAcquireResource(ConnectionResource resource, IAresDevice owner);

  /// <summary>
  /// Releases the lock on a resource held by a specific owner.
  /// </summary>
  /// <param name="resource">The resource to release.</param>
  /// <param name="owner">The owner releasing the resource.</param>
  void ReleaseResource(ConnectionResource resource, IAresDevice owner);

  /// <summary>
  /// Checks if a resource is currently in use.
  /// </summary>
  /// <param name="resource">The resource to check.</param>
  /// <returns>True if the resource is in use, false otherwise.</returns>
  bool IsResourceInUse(ConnectionResource resource);

  /// <summary>
  /// Gets the owner of the specified resource.
  /// </summary>
  /// <param name="resource">The resource to get the owner of.</param>
  /// <returns>The ResourceOwner, or null if the resource is not in use.</returns>
  List<IAresDevice> GetResourceOwners(ConnectionResource resource);
  }