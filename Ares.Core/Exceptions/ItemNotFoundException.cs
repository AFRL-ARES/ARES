namespace Ares.Core.Exceptions;
public class ItemNotFoundException : Exception
{
  public string? Id { get; }
  public Type? ItemType { get; }

  public ItemNotFoundException() { }

  public ItemNotFoundException(string message) : base(message) { }

  public ItemNotFoundException(string message, Exception innerException)
      : base(message, innerException) { }

  public ItemNotFoundException(string id, Type itemType, string? message = null)
      : base(message ?? $"Item of type '{itemType.Name}' with ID '{id}' not found.")
  {
    Id = id;
    ItemType = itemType;
  }
}