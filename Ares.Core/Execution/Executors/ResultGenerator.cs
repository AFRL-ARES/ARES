using Ares.Datamodel;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;

namespace Ares.Core.Execution.Executors;
internal static class ResultGenerator
{
  public static AresStruct GenerateExperimentResult(IEnumerable<StepExecutionSummary> steps, IEnumerable<StepTemplate> stepTemplates)
  {
    var commands = steps.SelectMany(step => step.CommandSummaries);
    var deviceResults = commands
      .Where(cmd => cmd.Result is not null && cmd.Result.Success)
      .Select(cmd => cmd.Result.Result)
      .OfType<AresStruct>();
    
    var experimentResultStruct = new AresStruct();

    if(!deviceResults.Any())
      return experimentResultStruct;

    var deviceResultStruct = deviceResults.Aggregate((total, next) => total.AppendStruct(next));

    var outputMaps = stepTemplates.SelectMany(st => st.CommandTemplates).Select(ct => ct.UserOutputKeyMap);
    var flattenedOutputMaps = outputMaps
      .SelectMany(map => map)
      .GroupBy(pair => pair.Key) // merge duplicates
      .ToDictionary(group => group.Key, group => group.Last().Value);

    foreach(var field in deviceResultStruct.Fields)
    {
      var found = flattenedOutputMaps.TryGetValue(field.Key, out var expOutputKey);
      if(found)
        experimentResultStruct.AddValue(expOutputKey!, field.Value);
    }

    return experimentResultStruct;
  }
}
