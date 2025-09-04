using Ares.Datamodel.Extensions;
using Ares.Datamodel.Templates;

namespace Ares.Core.AresEnvironment;

public class AresVariableManager
{
  public AresVariableManager()
  {
  }

  public bool TryResolveVariable(IEnumerable<Parameter> parameters)
  {
    var parameterArray = parameters.ToArray();

    foreach(var parameter in parameterArray)
    {
      if(!parameter.EnvironmentBased)
        continue;

      string? variableValue = null;

      if(parameter.VariableType is VariableType.PreviousExperimentPath)
        variableValue = GetPastExperimentPath(parameter.VariableArgument);

      else
        variableValue = AresEnvironment.GetEnvironmentVariable(parameter.VariableType);


      if(variableValue is null)
        return false;

      var val = AresValueHelper.CreateString(variableValue);

      parameter.Value = val;
    }

    return true;
  }

  public string? GetPastExperimentPath(string numberExperimentsBack)
  {
    var wasArgParsed = int.TryParse(numberExperimentsBack, out var parsedArg);

    var wasCurrentExpParsed = int.TryParse(AresEnvironment.GetInternalVariable(InternalVariableType.CurrentExperimentNumber), out var currentExperimentNumber);

    if(!wasArgParsed || !wasCurrentExpParsed)
      throw new InvalidOperationException("Failed to parse either current experiment number or user input!");

    var desiredExperimentNumber = currentExperimentNumber - parsedArg;
    var folderName = $"Experiment_{desiredExperimentNumber}";

    var campaignPath = AresEnvironment.GetEnvironmentVariable(VariableType.CampaignResultPath);

    if(campaignPath is null)
      throw new InvalidOperationException("Couldn't find current campaign path!");

    var fullPath = Path.Combine(campaignPath, folderName);

    //TODO: Make this not like this? This is a temporary fix to ensure our analyzer is capable of forwarding the image path forward.
    //realistically we should actually be saving this somewhere in the campaign as a result piece and then forwarding it to the analyzer that way.
    if(Directory.Exists(fullPath))
    {
      AresEnvironment.SetEnvironmentVariable(VariableType.PreviousExperimentPath, fullPath);
      return fullPath;
    }

    else
    {
      var path = AresEnvironment.GetEnvironmentVariable(VariableType.CampaignMiscFolder);
      AresEnvironment.SetEnvironmentVariable(VariableType.PreviousExperimentPath, path ?? string.Empty);
      return path;
    }

  }
}
