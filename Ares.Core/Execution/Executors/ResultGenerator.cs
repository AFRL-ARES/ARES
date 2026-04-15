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

    foreach(var cmd in successfulCommands)
    {
      if(!commandTemplates.TryGetValue(cmd.TemplateId, out var template) ||
          template.UserOutputKeyMap == null ||
          template.UserOutputKeyMap.Count == 0)
      {
        continue; // The user didn't map any outputs for this command. Skip it.
      }

      var aresValue = cmd.Result.Result;

      if(aresValue.KindCase == AresValue.KindOneofCase.StructValue && aresValue.StructValue is not null)
      {
        //Some legacy devices are going to continue to send structs with one value, handle those directly for now
        if(aresValue.StructValue.Fields.Count == 1)
          experimentResultStruct.Fields[template.UserOutputKeyMap.Values.First()] = aresValue.StructValue.Fields.First().Value;
        

        else
        {
          //This isn't being handled properly.. TODO: FIX
          foreach(var field in aresValue.StructValue.Fields)
          {
            if(template.UserOutputKeyMap.TryGetValue(field.Key, out var experimentOutputKey))
              experimentResultStruct.Fields[experimentOutputKey] = field.Value;
            
          }
        }
   
      }
      else if(aresValue.KindCase != AresValue.KindOneofCase.None)
      {
        if(template.UserOutputKeyMap.TryGetValue("Result", out var experimentOutputKey))
        {
          experimentResultStruct.Fields[experimentOutputKey] = aresValue;
        }

        else if(template.UserOutputKeyMap.Count == 1)
        {
          experimentResultStruct.Fields[template.UserOutputKeyMap.Values.First()] = aresValue;
        }
      }
    }

    return experimentResultStruct;
  }
}
