using Grpc.Core;
using System.Threading.Channels;

namespace UI.Infrastructure.Grpc;

public class LocalStream<T> : IAsyncStreamReader<T>, IServerStreamWriter<T>
{
  private readonly Channel<T> _channel = System.Threading.Channels.Channel.CreateUnbounded<T>();
  public T Current { get; private set; } = default!;

  public WriteOptions? WriteOptions { get; set; }

  public async Task<bool> MoveNext(CancellationToken cancellationToken)
  {
    try
    {
      if(await _channel.Reader.WaitToReadAsync(cancellationToken))
      {
        if(_channel.Reader.TryRead(out var item))
        {
          Current = item;
          return true;
        }
      }
    }
    catch(OperationCanceledException)
    {
    }
    return false;
  }

  public async Task WriteAsync(T message)
  {
    await _channel.Writer.WriteAsync(message);
  }

  public void Complete()
  {
    _channel.Writer.TryComplete();
  }
}
