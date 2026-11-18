namespace Ares.Core.CustomCommands.Exchange;

public sealed class CustomCommandImportException(string message, Exception? innerException = null) : Exception(message, innerException);