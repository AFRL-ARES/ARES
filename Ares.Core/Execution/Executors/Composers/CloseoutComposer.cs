using Ares.Messaging;

namespace Ares.Core.Execution.Executors.Composers
{
  public class CloseoutComposer : ICommandComposer<ExperimentTemplate, CloseoutScriptExecutor>
  {
    private readonly ICommandComposer<StepTemplate, StepExecutor> _stepComposer;

    public CloseoutComposer(ICommandComposer<StepTemplate, StepExecutor> stepComposer) 
    { 
      _stepComposer = stepComposer;
    } 
    public CloseoutScriptExecutor Compose(ExperimentTemplate template)
    {
      var closeoutExecutors =
        template
        .CloseoutStepTemplates
        .OrderBy(t => t.Index)
        .Select(_stepComposer.Compose)
        .ToArray();

      return new CloseoutScriptExecutor(template, closeoutExecutors);
    }
  }
}
