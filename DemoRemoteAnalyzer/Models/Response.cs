using Ares.Datamodel;
using Ares.Datamodel.Analyzing;

namespace DemoRemoteAnalyzer.Models;

public class Response
{
  public List<Objective> Objectives { get; set; }
  public Outcome Outcome { get; set; }
  public string ErrorString { get; set; }

  public Response(List<Objective> objectives, Outcome outcome, string errorString = "")
  {
    Objectives = objectives;
    Outcome = outcome;
    ErrorString = errorString;
  }
}
