using Ares.Core.Planning;
using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Planning;
using Ares.Datamodel.Templates;
using Ares.Device;
using BoraasPlanner.BoraasTypes;
using DynamicData;
using System.Text;
using System.Text.Json;

namespace BoraasPlanner;
public class BoraasPlanner : IPlanner
{
  readonly Uri _uri;

  public BoraasPlanner(Uri uri, string name)
  {
    _uri = uri;
    Address = uri.OriginalString;
    Name = name;
    Status = new PlannerStatus { PlannerState = PlannerState.Inactive, Message = "BORAAS Planner has not been activated!" };
  }

  public Task Init()
  {
    BoraasClientStore.CreateClient(_uri);

    var boraasPlanner = new Planner()
    {
      PlannerName = Name,
      Description = "The OSU Boraas Planner",
      UniqueId = UniqueId,
      Version = Version.ToString(),
    };

    //TODO: Actually check for connectivity? Though BORAAS seems to be a dead planner now.
    Status.PlannerState = PlannerState.Active;
    Status.Message = "Successfully activated BORAAS Planner!";

    AvailablePlanners.Add(boraasPlanner);

    return Task.CompletedTask;
  }

  public async Task<IEnumerable<Ares.Core.Planning.PlanResult>> Plan(IEnumerable<ParameterMetadata> plannableParameters,
    IEnumerable<ExperimentOverview> completedExperiments,
    IEnumerable<Analysis> experimentAnalyses,
    CancellationToken cancellationToken)
  {
    var boraasRequest = plannableParameters.ToBoraasRequest();

    AddExperimentHistory(boraasRequest, experimentAnalyses, plannableParameters, completedExperiments);

    var jsonRequest = JsonSerializer.Serialize(boraasRequest, new JsonSerializerOptions() { WriteIndented = true });

    var stringBoraasResponse = await RequestBoraasPlan(jsonRequest);

    if(stringBoraasResponse is null)
      return Enumerable.Empty<Ares.Core.Planning.PlanResult>();

    var resultsList = ConvertBoraasResponseToPlanResult(stringBoraasResponse, plannableParameters);

    return resultsList.Select(result => new Ares.Core.Planning.PlanResult(result.Metadata, AresValueHelper.CreateString(result.Value)));
  }

  public async Task<string?> RequestBoraasPlan(string jsonRequest)
  {
    var httpClient = new HttpClient(new HttpClientHandler() { UseDefaultCredentials = true }) { Timeout = TimeSpan.FromSeconds(30) };
    httpClient.BaseAddress = _uri;

    var httpRequest = new HttpRequestMessage(HttpMethod.Post, _uri) { Content = new StringContent(jsonRequest, Encoding.UTF8, "applications/json") };

    HttpResponseMessage? response;

    try
    {
      response = await httpClient.SendAsync(httpRequest);
    }

    catch(Exception e)
    {
      return null;
    }

    var reader = new StreamReader(response.Content.ReadAsStream());
    var responseBody = await reader.ReadToEndAsync();

    return responseBody;
  }

  private IEnumerable<PlanResult> ConvertBoraasResponseToPlanResult(string boraasStringResponse, IEnumerable<ParameterMetadata> metadata)
  {
    var boraasResponse = JsonSerializer.Deserialize<BoraasPlanResponse>(boraasStringResponse);

    var responsePrettified = JsonSerializer.Serialize(boraasResponse, new JsonSerializerOptions() { WriteIndented = true });

    var planResults = new List<PlanResult>();
    var parameters = boraasResponse.ParamNames!.ToList();
    var values = boraasResponse.Values![0].ToList();

    for(int i = 0; i < parameters.Count(); i++)
    {
      var result = new PlanResult();
      result.Value = values[i].ToString();

      var matchingMetadata = metadata.FirstOrDefault(data => data.Name == parameters[i]);

      if(matchingMetadata is not null)
        result.Metadata = matchingMetadata;

      planResults.Add(result);
    }

    return planResults;
  }

  private void AddExperimentHistory(BoraasPlanRequest request,
    IEnumerable<Analysis> analyses,
    IEnumerable<ParameterMetadata> plannableParameters,
    IEnumerable<ExperimentOverview> completedExperiments)
  {
    request.History = new List<List<double>>();

    if(analyses.Any())
    {
      foreach(var analysis in analyses)
      {
        var result = analysis.Result;
        var historyList = new List<double>() { result };

        foreach(var parameter in plannableParameters)
        {
          foreach(var completedExperiment in completedExperiments)
          {
            var matchingParam = completedExperiment.Parameters.FirstOrDefault(blah => blah.PlanningMetadata.Name == parameter.Name);

            if(matchingParam is null)
              throw new InvalidOperationException($"Couldn't find a matching parameter by the name of {parameter.Name}");

            if(!matchingParam.Value.HasNumberValue)
              continue;

            historyList.Add(matchingParam.Value.NumberValue);
          }
        }
        request.History.Add(historyList);
      }
    }
  }

  public Task<IEnumerable<Ares.Core.Planning.PlanResult>> Plan(IEnumerable<ParameterMetadata> plannableParameters, IEnumerable<Analysis> experimentAnalyses, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public string Name { get; set; }
  public Version Version { get; set; } = new Version(1, 0);
  public string Address { get; set; }
  public string UniqueId { get; set; } = Guid.NewGuid().ToString();
  public IList<Planner> AvailablePlanners { get; } = new List<Planner>();
  public PlannerStatus Status { get; }
  public IList<PlannerSetting> AdapterSettings { get; set; } = new List<PlannerSetting>();
}
