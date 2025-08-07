using Ares.Core.Analyzing;
using Ares.Datamodel;
using Ares.Datamodel.Templates;

namespace Ares.Core.Execution.Executors.Composers;

public class ExperimentComposer : ICommandComposer<ExperimentTemplate, ExperimentExecutor>
{
  private readonly ICommandComposer<StepTemplate, StepExecutor> _stepComposer;
  private readonly IAnalyzerRepo _analyzerManager;

  public ExperimentComposer(ICommandComposer<StepTemplate, StepExecutor> stepComposer, IAnalyzerRepo analyzerManager)
  {
    _stepComposer = stepComposer;
    _analyzerManager = analyzerManager;
  }

  public ExperimentExecutor Compose(ExperimentTemplate template)
  {
    var stepExecutors =
      template
        .StepTemplates
        .OrderBy(t => t.Index)
        .Select(_stepComposer.Compose)
        .ToArray();

    var closeoutExecutors = 
      template
      .CloseoutStepTemplates
      .OrderBy(t => t.Index)
      .Select(_stepComposer.Compose)
      .ToArray();



    return new ExperimentExecutor(template, stepExecutors);
  }

  public ExperimentExecutor Compose(ExperimentTemplate template, ExperimentExecutionStatus experimentExecutionStatus)
  {
    throw new NotImplementedException();
  }
}
