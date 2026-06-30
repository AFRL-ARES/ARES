using Ares.Datamodel;
using Ares.Datamodel.Templates;

namespace Ares.Core.Execution.Executors.Composers;

public interface IExecutionNodeComposer
{
  IExecutor<CommandExecutionSummary, CommandExecutionStatus> ComposeSequence(string id, string name, IEnumerable<ExecutionNode> nodes);
}
