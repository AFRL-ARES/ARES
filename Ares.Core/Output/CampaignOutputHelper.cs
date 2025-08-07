using System.Reflection;
using Ares.Core.Analyzing;
using Ares.Datamodel;
using Ares.Datamodel.Templates;

namespace Ares.Core.Output;

public static class CampaignOutputHelper
{
  public static Task<string> InitializeOutputDirectories(CampaignTemplate template, DateTime startTime)
  {
    //Create Results Directory
    var campaignPath = CreateCampaignResultsFolder(template.Name, startTime);
    AresEnvironment.AresEnvironment.SetEnvironmentVariable(VariableType.CampaignResultPath, campaignPath);

    //Create Miscellaneous Folder
    var miscFolderPath = CreateCampaignMiscellaneousFolder(campaignPath);
    AresEnvironment.AresEnvironment.SetEnvironmentVariable(VariableType.CampaignMiscFolder, miscFolderPath);

    //Create Startup Folder
    var startupFolder = CreateStartupSubFolder(campaignPath, "Startup");
    AresEnvironment.AresEnvironment.SetEnvironmentVariable(VariableType.CampaignStartupFolder, startupFolder);

    //Set Internal Variables related to Campaign
    AresEnvironment.AresEnvironment.SetInternalVariable(InternalVariableType.CurrentCampaignId, template.UniqueId);
    AresEnvironment.AresEnvironment.SetInternalVariable(InternalVariableType.CurrentCampaignName, template.Name);

    return Task.FromResult(campaignPath);
  }

  public static async Task WriteExperimentNotes(string campaignPath, string notes)
  {
    var path = Path.Combine(campaignPath, "ExecutionNotes.txt");
    await File.WriteAllTextAsync(path, notes);
  }

  public static async Task WriteExperimentTags(string campaignPath, List<AresCampaignTag> tags)
  {
    var tag_names = tags.Select(t => t.TagName);
    var path = Path.Combine(campaignPath, "ExecutionTags.txt");
    await File.WriteAllTextAsync(path, string.Join(",", tag_names));
  }

  private static string CreateCampaignMiscellaneousFolder(string campaignPath)
  {
    var newFolderPath = Path.Combine(campaignPath, "Miscellaneous");
    Directory.CreateDirectory(newFolderPath);
    return newFolderPath;
  }

  public static string CreateExperimentSubFolder(string camapignPath, string folderName)
  {
    var experimentPath = Path.Combine(camapignPath, folderName);
    Directory.CreateDirectory(experimentPath);
    AresEnvironment.AresEnvironment.SetEnvironmentVariable(VariableType.ExperimentResultPath, experimentPath);
    return experimentPath;
  }

  private static string CreateStartupSubFolder(string campaignPath, string folderName)
  {
    var startupPath = Path.Combine(campaignPath, folderName);
    Directory.CreateDirectory(startupPath);
    return startupPath;
  }

  private static string CreateCampaignResultsFolder(string campaignName, DateTime startTime)
  {
    var newFolderName = $"{campaignName}_{startTime.ToString("_yyyy-MM-dd_HH-mm-ss")}";
    var validatedFolderName = EnsureValidFolderName(newFolderName);
    var fullPath = Path.Combine(AresConfig.ResultsPath, validatedFolderName);
    Directory.CreateDirectory(fullPath);
    return fullPath;
  }

  private static string EnsureValidFolderName(string folderName)
  {
    foreach(var c in Path.GetInvalidFileNameChars())
      folderName = folderName.Replace(c.ToString(), string.Empty);
    
    return folderName;
  }

  public static async Task OutputVersionFile(string campaignPath, CampaignTemplate template, IAnalyzer? analyzer)
  {
    var versionedItems = new Dictionary<string, string>();
    var path = Path.Combine(campaignPath, "Version.txt");
    var experimentTemplate = template.ExperimentTemplates.First();

    if(analyzer is not null)
      versionedItems.Add(analyzer.Name, analyzer.Version);

    foreach(var allocation in template.PlannerAllocations)
    {
      var found = versionedItems.TryGetValue(allocation.Planner.AdapterName, out var value);

      if(!found)
        versionedItems.Add(allocation.Planner.AdapterName, allocation.Planner.Version);
    }

    var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
    var assemblyVersion = assembly.GetName().Version;
    var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    if(informationalVersion is not null)
      versionedItems.Add("AresCore", informationalVersion);

    foreach(var (key, value) in versionedItems)
      await File.AppendAllTextAsync(path, $"{key} -- {value}\r\n");
  }
}
