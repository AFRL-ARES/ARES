namespace Ares.Device.Serial.Commands;

public record SerialTransaction<TResponse>(SerialCommandWithResponse<TResponse> Request, TResponse Response) where TResponse : SerialResponse;
