using Ares.Datamodel;
using Ares.Datamodel.Templates;

namespace Ares.Core.Execution.Executors;
internal static class ResultGenerator
{
  public static AresStruct GenerateExperimentResult(IEnumerable<StepExecutionSummary> steps, IEnumerable<StepTemplate> stepTemplates)
  {
    var experimentResultStruct = new AresStruct();

    // 1. Create a fast lookup dictionary for our templates so we can isolate the mappings
    var commandTemplates = stepTemplates
        .SelectMany(st => st.CommandTemplates)
        .ToDictionary(ct => ct.UniqueId);

    var successfulCommands = steps
        .SelectMany(step => step.CommandSummaries)
        .Where(cmd => cmd.Result?.Success == true && cmd.Result.Result is not null);

    foreach(var cmd in successfulCommands)
    {
      // 2. Look up the specific template that generated this command
      if(!commandTemplates.TryGetValue(cmd.TemplateId, out var template) ||
          template.UserOutputKeyMap == null ||
          template.UserOutputKeyMap.Count == 0)
      {
        continue; // The user didn't map any outputs for this command. Skip it.
      }

      var aresValue = cmd.Result.Result;

      // 3. Handle Structs (The "Cherry-Picker" for Live Data)
      if(aresValue.KindCase == AresValue.KindOneofCase.StructValue && aresValue.StructValue is not null)
      {
        foreach(var field in aresValue.StructValue.Fields)
        {
          // Check if the user specifically asked for this field (e.g., "Temperature")
          if(template.UserOutputKeyMap.TryGetValue(field.Key, out var experimentOutputKey))
          {
            // They did! Add it to the final result using their custom name.
            experimentResultStruct.Fields[experimentOutputKey] = field.Value;
          }
        }
      }
      // 4. Handle Primitives (The "Simple UI" for Setpoints, etc.)
      else if(aresValue.KindCase != AresValue.KindOneofCase.None)
      {
        // First, try to find our standardized fallback key
        if(template.UserOutputKeyMap.TryGetValue("Result", out var experimentOutputKey))
        {
          experimentResultStruct.Fields[experimentOutputKey] = aresValue;
        }
        // BULLETPROOF FALLBACK: If the UI didn't save it as "Result" but there is only 
        // 1 mapping defined for this primitive command, just blindly use that user-defined name!
        else if(template.UserOutputKeyMap.Count == 1)
        {
          experimentResultStruct.Fields[template.UserOutputKeyMap.Values.First()] = aresValue;
        }
      }
    }

    return experimentResultStruct;
  }
}
