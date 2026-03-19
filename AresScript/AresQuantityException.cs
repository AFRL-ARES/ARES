namespace AresScript;

/// <summary>
/// Thrown by <see cref="QuantityUnitHelper"/> when a quantity operation is invalid
/// (e.g. incompatible units). The interpreter catches this and enhances it with
/// source location information.
/// </summary>
public sealed class AresQuantityException(string message) : InvalidOperationException(message);
