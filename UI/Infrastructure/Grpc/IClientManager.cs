using Grpc.Core;

namespace UI.Infrastructure.Grpc;

public interface IClientManager
{
  public T GetClient<T>() where T : ClientBase<T>;
}
