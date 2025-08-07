using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;
using System.Reactive.Linq;

namespace Ares.Core.Planning;

public class ManualPlanner : IPlanner
{
  private readonly Queue<IEnumerable<ManualPlanResult>> _planResultsQueue = new();

  public ManualPlanner()
  {
    Status = new PlannerStatus { PlannerState = PlannerState.Active, Message = "Manual Planner is active!" };
  }

  public IEnumerable<IEnumerable<(string Name, AresValue Value)>> CurrentPlanResults => 
    _planResultsQueue
    .AsEnumerable()
    .Select(results => results
    .Select(result => (result.Name, result.value)));


  public Task<IEnumerable<PlanResult>> Plan(IEnumerable<ParameterMetadata> plannableParameters, 
    IEnumerable<CompletedExperiment> experiments, 
    IEnumerable<Analysis> _, 
    CancellationToken __)
  {
    try
    {
      var currentParameterSet = _planResultsQueue.Dequeue().ToList();
      var returnList = plannableParameters.Select(metadata => currentParameterSet.First(result => result.Name == metadata.Name).ToPlanResult(metadata));
      return Task.FromResult(returnList);
    }
    catch(InvalidOperationException)
    {
      return Task.FromResult<IEnumerable<PlanResult>>(new List<PlanResult>());
    }
  }

  public Task Seed(ManualPlannerSeed seedParam)
  {
    Reset();
    switch(seedParam.PlannerStuffCase)
    {
      case ManualPlannerSeed.PlannerStuffOneofCase.None:
        break;
      case ManualPlannerSeed.PlannerStuffOneofCase.PlannerValues:
        var manualPlanResultCollections = seedParam.PlannerValues.PlannedValues
          .Select(set => set.ParameterValues
          .Select(pair => new ManualPlanResult(pair.Name, pair.Value)));

        foreach(var manualPlanResults in manualPlanResultCollections)
          _planResultsQueue.Enqueue(manualPlanResults);

        break;
      case ManualPlannerSeed.PlannerStuffOneofCase.FileLines:
        LoadPlanQueue(seedParam.FileLines.PlannerValues);
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }

    return Task.CompletedTask;
  }

  public void Reset()
  {
    _planResultsQueue.Clear();
  }

  public Task Init()
  {
    var manualPlanner = new Planner()
    {
      PlannerName = "Manual Planner",
      Description = "A planner used for executing sets of manual values.",
      UniqueId = UniqueId,
      Version = Version.ToString()
    };

    AvailablePlanners.Add(manualPlanner);
    return Task.CompletedTask;
  }

  private void LoadPlanQueue(IEnumerable<string> lines, char delim = ',')
  {
    // Read all lines from the data file
    List<string> dataFileLines = lines.ToList();

    // Make sure the data file has data!
    if(dataFileLines == null || dataFileLines.Count < 2)
      throw new Exception("Could not read any data from file!");

    // Create a useful Func to split lines
    var tokenizeLine = new Func<string, List<string>>(line =>
    {
      return line.Trim().Split(new[] { delim }, StringSplitOptions.RemoveEmptyEntries).ToList();
    });

    // Tokenize the first line of the file
    List<string> firstLineTokens = tokenizeLine(dataFileLines.First());

    // Ensure the validity of the data descriptions then remove them from the list
    firstLineTokens.ForEach(desc =>
    {
      if(string.IsNullOrEmpty(desc.Trim()))
        throw new Exception("Data descriptions in file cannot be null or empty!");
    });

    dataFileLines.RemoveAt(0);

    // Tokenize each line and parse to doubles
    int expNum = 1;// 1 based index
    List<List<string>> data = new List<List<string>>();
    dataFileLines.ForEach(expDataLine =>
    {
      // Tokenize the line and check the validity of it
      expNum += 1;
      List<string> expLineTokens = tokenizeLine(expDataLine);
      if(expLineTokens.Count != firstLineTokens.Count)
        throw new Exception("Experiment " + expNum + " does not have enough tokens in line!");

      // Parse the tokens to double and check the validities
      int tokenNum = 0;// 1 based index
      List<string> expData = new();
      expLineTokens.ForEach(dataToken =>
      {
        tokenNum += 1;
        if(string.IsNullOrEmpty(dataToken.Trim()))
          throw new Exception("Experiment " + expNum + ", column " + tokenNum + " data is null or empty!");

        expData.Add(dataToken);
      });

      // This line is a good experiment
      data.Add(expData);
    });

    foreach(var argList in data)
    {
      var planResults = argList.Select((d, i) => new ManualPlanResult(firstLineTokens[i], AresValueHelper.CreateString(d)));
      _planResultsQueue.Enqueue(planResults);
    }
  }

  private record ManualPlanResult(string Name, AresValue value)

  {
    public PlanResult ToPlanResult(ParameterMetadata metadata)
      => new(metadata, value);
  }

  public PlannerStatus Status { get; protected set; }
  public string Name { get; set; } = "Manual Planner";
  public Version Version { get; set; } = new(1, 0);
  public string Address { get; set; } = string.Empty;
  public string UniqueId { get; set; } = new Guid().ToString();
  public IList<Planner> AvailablePlanners { get; } = new List<Planner>();
  public IList<PlannerSetting> AdapterSettings { get; } = new List<PlannerSetting>();
}
