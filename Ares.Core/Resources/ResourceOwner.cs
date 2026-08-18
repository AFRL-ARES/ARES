namespace Ares.Core.Resources
{
    /// <summary>
    /// Represents the owner of a shared resource.
    /// </summary>
    /// <param name="OwnerId">The unique identifier of the owner.</param>
    /// <param name="OwnerType">The type of the owner (e.g., "Device", "Analyzer").</param>
    public record ResourceOwner(string OwnerId, string OwnerType);
}
