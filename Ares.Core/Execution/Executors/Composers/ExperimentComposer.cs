using Ares.Messaging;

namespace Ares.Core.Execution.Executors.Composers;

public class ExperimentComposer : ICommandComposer<ExperimentTemplate, ExperimentExecutor>
{
  private readonly ICommandComposer<StepTemplate, StepExecutor> _stepComposer;

  public ExperimentComposer(ICommandComposer<StepTemplate, StepExecutor> stepComposer)
  {
    _stepComposer = stepComposer;
  }

  public ExperimentExecutor Compose(ExperimentTemplate template)
  {
    var stepExecutors =
      template
        .StepTemplates
        .OrderBy(t => t.Index)
        .Select(_stepComposer.Compose)
        .ToArray();

    return new ExperimentExecutor(template, stepExecutors);
  }
}
