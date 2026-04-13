using Ares.Datamodel;
using AresScript.Environment;

namespace Ares.Core.Execution.Executors;

internal static class ResultGenerator
{
  public static AresValue GetExperimentResult(AresScriptEnvironment env)
  {
    if(!env.TryGetValue("__experiment_result", out var val))
    {
      throw new InvalidOperationException("Experiment result has not been defined");
    }

    return val;
  }
}
