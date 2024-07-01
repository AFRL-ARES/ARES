using Grpc.Core;

namespace UI.Services.Grpc;

public interface IClientManager
{
  public T GetClient<T>() where T : ClientBase<T>;
}
