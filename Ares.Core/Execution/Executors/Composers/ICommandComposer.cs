using Google.Protobuf;

namespace Ares.Core.Execution.Executors.Composers;

public interface ICommandComposer<in TDbTemplate, out TExecutor>
  where TDbTemplate : IMessage
{
  TExecutor Compose(TDbTemplate template);
}
