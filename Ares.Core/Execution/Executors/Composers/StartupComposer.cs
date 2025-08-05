using Ares.Messaging;

namespace Ares.Core.Execution.Executors.Composers
{
  public class StartupComposer : ICommandComposer<ExperimentTemplate, StartupScriptExecutor>
  {
    private readonly ICommandComposer<StepTemplate, StepExecutor> _stepComposer;

    public StartupComposer(ICommandComposer<StepTemplate, StepExecutor> stepComposer)
    {
      _stepComposer = stepComposer;
    }

    public StartupScriptExecutor Compose(ExperimentTemplate template)
    {
      var startupExecutors =
        template
        .StartupStepTemplates
        .OrderBy(t => t.Index)
        .Select(_stepComposer.Compose)
        .ToArray();

      return new StartupScriptExecutor(template, startupExecutors);
    }
  }
}
