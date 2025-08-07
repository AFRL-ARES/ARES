using Ares.Datamodel.Templates;
using BoraasPlanner.BoraasTypes;

namespace BoraasPlanner;
public static class BoraasRequestFactory
{
  public static BoraasPlanRequest ToBoraasRequest(this IEnumerable<ParameterMetadata> enumerableMetadata)
  {
    var paramNames = new List<string>() { "Result" };
    var solveFor = new List<string>() { "Result" };
    var minVals = new List<double>();
    var maxVals = new List<double>();

    minVals.Add(0);
    maxVals.Add(10000);

    foreach(var parameter in enumerableMetadata)
    {
      paramNames.Add(parameter.Name);
      minVals.Add(parameter.Constraints[0].Minimum);
      maxVals.Add(parameter.Constraints[0].Maximum);
    }

    var request = new BoraasPlanRequest()
    {
      ParamNames = paramNames,
      SolveFor = solveFor,
      MinVals = minVals,
      MaxVals = maxVals,
      Key = "MOBOCNT",
      NumExpsRequested = 1,
      Test = true
    };

    return request;
  }
}
