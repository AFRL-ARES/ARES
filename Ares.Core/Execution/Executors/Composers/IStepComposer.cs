using Ares.Datamodel;
using Ares.Datamodel.Templates;

namespace Ares.Core.Execution.Executors.Composers;

public interface IStepComposer
{
  IEnumerable<IExecutor<CommandExecutionSummary, CommandExecutionStatus>> ComposeNodes(IEnumerable<ExecutionNode> template, bool isParallel);
}
