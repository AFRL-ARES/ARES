namespace Ares.Core.Resources
{
    /// <summary>
    /// Defines the types of connections a resource can have.
    /// </summary>
    public enum ConnectionType
    {
        /// <summary>
        /// A serial port connection (e.g., COM1).
        /// </summary>
        Serial,
        /// <summary>
        /// A Universal Serial Bus connection.
        /// </summary>
        USB,
        /// <summary>
        /// A network connection over Ethernet.
        /// </summary>
        Ethernet,
        /// <summary>
        /// A connection type that is not otherwise specified.
        /// </summary>
        Other
    }
}
