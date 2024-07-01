using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ares.Messaging;
using Ares.Messaging.Planning;

namespace Ares.Core.Planning;

public class ManualPlanner : IPlanner
{
  private readonly ISubject<PlannerState> _plannerStateSubject = new BehaviorSubject<PlannerState>(Planning.PlannerState.Disconnected);
  private readonly Queue<IEnumerable<ManualPlanResult>> _planResultsQueue = new();

  public ManualPlanner()
  {
    PlannerState = _plannerStateSubject.AsObservable();
  }

  public IEnumerable<IEnumerable<(string Name, double Value)>> CurrentPlanResults => _planResultsQueue.AsEnumerable().Select(results => results.Select(result => (result.Name, result.Value)));

  public string Name { get; } = "Manual Planner";
  public Version Version { get; } = new(1, 0);

  public Task<IEnumerable<PlanResult>> Plan(IEnumerable<ParameterMetadata> plannableParameters, IEnumerable<Analysis> _, CancellationToken __)
  {
    try
    {
      var currentParameterSet = _planResultsQueue.Dequeue().ToList();
      var returnList = plannableParameters.Select(metadata => currentParameterSet.First(result => result.Name == metadata.Name).ToPlanResult(metadata));
      return Task.FromResult(returnList);
    }
    catch (InvalidOperationException)
    {
      return Task.FromResult<IEnumerable<PlanResult>>(new List<PlanResult>());
    }
  }

  public IObservable<PlannerState> PlannerState { get; }

  public Task Seed(ManualPlannerSeed seedParam)
  {
    Reset();
    switch (seedParam.PlannerStuffCase)
    {
      case ManualPlannerSeed.PlannerStuffOneofCase.None:
        break;
      case ManualPlannerSeed.PlannerStuffOneofCase.PlannerValues:
        var manualPlanResultCollections = seedParam.PlannerValues.PlannedValues.Select(set => set.ParameterValues.Select(pair => new ManualPlanResult(pair.Name, pair.Value)));
        foreach (var manualPlanResults in manualPlanResultCollections)
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
    _plannerStateSubject.OnNext(Planning.PlannerState.Connected);
    return Task.CompletedTask;
  }

  private void LoadPlanQueue(IEnumerable<string> lines, char delim = ',')
  {
    // Read all lines from the data file
    List<string> dataFileLines = lines.ToList();

    // Make sure the data file has data!
    if (dataFileLines == null || dataFileLines.Count < 2)
      throw new Exception("Could not read any data from file!");

    // Create a useful Func to split lines
    var tokenizeLine = new Func<string, List<string>>(line => {
      return line.Trim().Split(new[] { delim }, StringSplitOptions.RemoveEmptyEntries).ToList();
    });

    // Tokenize the first line of the file
    List<string> firstLineTokens = tokenizeLine(dataFileLines.First());

    // Ensure the validity of the data descriptions then remove them from the list
    firstLineTokens.ForEach(desc => {
      if (string.IsNullOrEmpty(desc.Trim()))
        throw new Exception("Data descriptions in file cannot be null or empty!");
    });

    dataFileLines.RemoveAt(0);

    // Tokenize each line and parse to doubles
    int expNum = 1;// 1 based index
    List<List<double>> data = new List<List<double>>();
    dataFileLines.ForEach(expDataLine => {
      // Tokenize the line and check the validity of it
      expNum += 1;
      List<string> expLineTokens = tokenizeLine(expDataLine);
      if (expLineTokens.Count != firstLineTokens.Count)
        throw new Exception("Experiment " + expNum + " does not have enough tokens in line!");

      // Parse the tokens to double and check the validities
      int tokenNum = 0;// 1 based index
      List<double> expData = new List<double>();
      expLineTokens.ForEach(dataToken => {
        tokenNum += 1;
        if (string.IsNullOrEmpty(dataToken.Trim()))
          throw new Exception("Experiment " + expNum + ", column " + tokenNum + " data is null or empty!");

        double dataVal;
        try
        {
          dataVal = Convert.ToDouble(dataToken);
        }
        catch (Exception)
        {
          throw new Exception("Experiment " + expNum + ", column " + tokenNum + " data cannot be parsed to a double!");
        }

        if (double.IsInfinity(dataVal) || double.IsNaN(dataVal))
          throw new Exception("Experiment " + expNum + ", column " + tokenNum + " data cannot be NaN or an Infinity!");

        // This token is good data
        expData.Add(dataVal);
      });

      // This line is a good experiment
      data.Add(expData);
    });

    foreach (var argList in data)
    {
      var planResults = argList.Select((d, i) => new ManualPlanResult(firstLineTokens[i], d));
      _planResultsQueue.Enqueue(planResults);
    }
  }

  //
  private record ManualPlanResult(string Name, double Value)
  {
    public PlanResult ToPlanResult(ParameterMetadata metadata)
      => new(metadata, Value);
  }
}
