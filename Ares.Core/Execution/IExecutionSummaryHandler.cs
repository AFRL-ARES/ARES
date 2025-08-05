using Ares.Messaging;

namespace Ares.Core.Execution;
public interface IExecutionSummaryHandler
{
  Task Handle(ExperimentExecutionSummary result);
}
