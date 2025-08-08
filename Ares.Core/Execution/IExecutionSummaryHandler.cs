using Ares.Datamodel;

namespace Ares.Core.Execution;
public interface IExecutionSummaryHandler
{
  Task Handle(ExperimentExecutionSummary result);
}
