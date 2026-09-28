using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Analyzing.Remote;
using Ares.Datamodel.Extensions;
using DemoRemoteAnalyzer.Models;
using Google.Protobuf.Collections;
using System.Text.Json;

namespace DemoRemoteAnalyzer.Services;

/// <summary>
/// This is a C# based implementation of the Demo Response Surface Analyzer created by Arthur W. N. Sloan.
/// The original code for this logic can be found by visting https://github.com/AFRL-ARES/pyares-demo-response
/// </summary>
public class DemoResponseSurfaceAnalyzer
{
  private static List<SyntheticProcessResponse> _responseSpaces = new List<SyntheticProcessResponse>();
  private static List<Dictionary<string, double>> _previousPoints = new List<Dictionary<string, double>>();
  private static List<string> _responseNames = new List<string>();
  private static int? _currentRngSeed = null;

  public Config Cfg { get; set; } = new Config();

  /// <summary>
  /// Process an analysis request and generate a synthetic process response.
  /// </summary>
  public AnalysisResponse? DemoResponse(AnalysisRequest request)
  {
    if(request is null)
      return null;

    Console.WriteLine("Received Requests with inputs:");
    if(request?.Inputs != null)
    {
      foreach(var entry in request.Inputs.Fields)
      {
        Console.WriteLine($"\t{entry.Key}: {entry.Value}");
      }
    }

    try
    {
      // Extract inputs and settings from request
      var inputDict = request!.Inputs.Fields ?? new MapField<string, AresValue>();
      var convertedInputDict = inputDict.ToDictionary(kvp => kvp.Key, kvp => ExtractNumericValue(kvp.Value));
      var inputNames = inputDict.Keys.ToList();

      // Handle random seed configuration (user provided or Unix timestamp)
      int rngSeed;

      if(request.Settings != null && request.Settings.Fields.TryGetValue("RNG Seed", out var seedAresValue))
      {
        var numericValueFound = seedAresValue.TryGetNumericValue(out var numericValue);
        
        if(!numericValueFound)
          rngSeed = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        else
          rngSeed = (int)numericValue;
      }
     
      else
      {
        rngSeed = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Console.WriteLine("No RNG seed value received, using the current time stamp");
      }

      // Handle input bounds configuration
      Dictionary<string, List<double>> inputBounds;
      if(request.Settings != null && request.Settings.Fields.TryGetValue("Input Bounds", out var boundsAresValue))
      {
        var boundsStr = boundsAresValue.StringValue;
        inputBounds = JsonSerializer.Deserialize<Dictionary<string, List<double>>>(boundsStr);
      }

      else
      {
        Console.WriteLine("No input bounds value received, using the range [0,1] for all inputs");
        inputBounds = inputNames.ToDictionary(n => n, n => new List<double> { 0.0, 1.0 });
      }

      var objectives = Cfg.Objectives ?? new List<ObjectiveSchema>();
      int nResponses = objectives.Count;
      _responseNames = objectives.Select(o => o.ObjectiveName).ToList();

      // Handle output bounds configuration
      List<double> outputBounds;
      if(request.Settings != null && request.Settings.Fields.TryGetValue("Output Bounds", out var outputBoundsAresValue))
      {
        var outputBoundsStr = outputBoundsAresValue.StringValue ?? "";
        outputBounds = JsonSerializer.Deserialize<List<double>>(outputBoundsStr);
      }
      else
      {
        outputBounds = new List<double> { 0.0, 1.0 };
      }

      // Ensure all input names have bounds defined
      foreach(var name in inputNames)
      {
        if(!inputBounds.ContainsKey(name))
        {
          inputBounds[name] = new List<double> { 0.0, 1.0 };
        }
      }

      // Re-create response spaces if count or RNG seed has changed
      if(_responseSpaces.Count != nResponses || rngSeed != _currentRngSeed)
      {
        _responseSpaces.Clear();
        _previousPoints.Clear();

        var configDict = inputNames.ToDictionary(n => n, n => inputBounds[n]);
        var rng = new Random(rngSeed);
        _currentRngSeed = rngSeed;

        for(int i = 0; i < nResponses; i++)
        {
          int numGaussians = rng.Next(4, 12); // upper bound is exclusive
          double noiseScale = rng.NextDouble() * (0.2 - 0.05) + 0.05;
          double noiseFrequency = rng.NextDouble() * (10.0 - 1.0) + 1.0;
          long seed = (long)(rng.NextDouble() * (1e12 - 1e6) + 1e6);

          _responseSpaces.Add(new SyntheticProcessResponse(
              configDict,
              outputBounds,
              numGaussians,
              noiseScale,
              noiseFrequency,
              seed
          ));
        }
      }

      var analysisResults = new List<Objective>();
      var iterationPoints = new List<double>();
      var currentPoint = new Dictionary<string, double>(convertedInputDict);

      for(int i = 0; i < _responseSpaces.Count; i++)
      {
        double result = _responseSpaces[i].Evaluate(convertedInputDict);
        iterationPoints.Add(result);
        currentPoint[_responseNames[i]] = result;

        Console.WriteLine($"{_responseNames[i]}: {result}");
        analysisResults.Add(new Objective() { ObjectiveName = _responseNames[i], ObjectiveValue = AresValueHelper.CreateFloat(result) });
      }

      _previousPoints.Add(currentPoint);

      var analysisResponse = new AnalysisResponse() { AnalysisOutcome = Outcome.Success, ErrorString = string.Empty };
      analysisResponse.Objectives.AddRange(analysisResults);

      return analysisResponse;
    }
    catch(Exception e)
    {
      Console.WriteLine($"Error processing request: {e.Message}");
      var failedObjectives = _responseNames.Select(name => new Objective() { ObjectiveName = name, ObjectiveValue = AresValueHelper.CreateFloat(-1.0)}).ToList();

      var analysisResponse = new AnalysisResponse { AnalysisOutcome = Outcome.Failure, ErrorString = e.Message };
      analysisResponse.Objectives.AddRange(failedObjectives);

      return analysisResponse;
    }
  }

  private double ExtractNumericValue(AresValue value)
  {
    var numericFound = value.TryGetNumericValue(out var number);

    if(numericFound)
      return number;

    else
      return double.NaN;
  }
}
