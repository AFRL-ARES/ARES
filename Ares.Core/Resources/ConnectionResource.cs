namespace Ares.Core.Resources
{
    /// <summary>
    /// Represents a shared connection resource.
    /// </summary>
    /// <param name="ResourceName">The name of the resource (e.g., "COM1").</param>
    /// <param name="Type">The type of connection.</param>
    public record ConnectionResource(string ResourceName, ConnectionType Type);
}
