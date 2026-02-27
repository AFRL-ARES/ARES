using Grpc.Core;

namespace UI.Infrastructure.Grpc;

public class LocalStreamWriter<T>(Func<T, Task> onWrite) : IServerStreamWriter<T>
{
  public WriteOptions? WriteOptions { get; set; }

  public async Task WriteAsync(T message)
  {
    await onWrite(message);
  }
}
