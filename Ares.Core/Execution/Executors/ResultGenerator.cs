using Ares.Datamodel;
using Ares.Datamodel.Templates;

namespace Ares.Core.Execution.Executors;

internal static class ResultGenerator
{
  public static AresStruct GenerateExperimentResult(IEnumerable<StepExecutionSummary> steps, IEnumerable<StepTemplate> stepTemplates)
  {
    var experimentResultStruct = new AresStruct();

    var commandTemplates = stepTemplates
        .SelectMany(st => st.CommandTemplates)
        .ToDictionary(ct => ct.UniqueId);

    var successfulCommands = steps
        .SelectMany(step => step.CommandSummaries)
        .Where(cmd => cmd.Result?.Success == true && cmd.Result.Result is not null);

    var dict = successfulCommands
      .Where(cmd => cmd.HasVarName && cmd.Result.Result.KindCase != AresValue.KindOneofCase.None)
      .Select(cmd => KeyValuePair.Create(cmd.VarName, cmd.Result.Result))
      .ToDictionary();

    experimentResultStruct.Fields.Add(dict);

    return experimentResultStruct;
  }
}
