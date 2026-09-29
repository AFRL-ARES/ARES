using Ares.Datamodel;
using Ares.Datamodel.Analyzing;
using Ares.Datamodel.Analyzing.Remote;
using Ares.Datamodel.Extensions;
using DemoRemoteAnalyzer.Models;
using DemoRemoteAnalyzer.Tools;
using Google.Protobuf.Collections;
using System.Text.Json;

namespace DemoRemoteAnalyzer.Services;

/// <summary>
/// Generates synthetic response surfaces for the demo analyzer.
/// </summary>
public class DemoResponseSurfaceAnalyzer
{
  private readonly List<SyntheticProcessResponse> _responseSpaces = new();
  private readonly List<Dictionary<string, double>> _previousPoints = new();
  private List<string> _responseNames = new();
  private ulong? _currentRngSeed;
  private string? _currentConfigurationKey;
  private readonly object _sync = new();

  private Pcg64 _randomizer = new((ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds());

  public Config Cfg { get; set; } = new();

  /// <summary>
  /// Processes an analysis request and generates synthetic objective values.
  /// </summary>
  public AnalysisResponse? DemoResponse(AnalysisRequest request)
  {
    if(request is null)
    {
      return null;
    }

    lock(_sync)
    {
      try
      {
        Console.WriteLine("Received Requests with inputs:");
        foreach(var entry in request.Inputs.Fields)
        {
          Console.WriteLine($"\t{entry.Key}: {entry.Value}");
        }

        var inputDict = request.Inputs.Fields ?? new MapField<string, AresValue>();
        var convertedInputDict = inputDict.ToDictionary(pair => pair.Key, pair => ExtractNumericValue(pair.Value));
        var inputNames = inputDict.Keys.ToList();

        if(inputNames.Count == 0)
          throw new FormatException("At least one analyzer input is required.");

        var rngSeed = ReadSeed(request);
        var inputBounds = ReadInputBounds(request, inputNames);
        var outputBounds = ReadOutputBounds(request);

        var objectives = Cfg.Objectives ?? new List<ObjectiveSchema>();
        if(objectives.Count == 0)
          throw new FormatException("At least one analyzer objective is required.");

        _responseNames = objectives.Select(objective => objective.ObjectiveName).ToList();
        if(_responseNames.Any(string.IsNullOrWhiteSpace) || _responseNames.Distinct(StringComparer.Ordinal).Count() != _responseNames.Count)
          throw new FormatException("Objective names must be nonempty and unique.");

        foreach(var name in inputNames)
        {
          if(!inputBounds.ContainsKey(name))
            inputBounds[name] = new List<double> { 0.0, 1.0 };

          ValidateBounds(name, inputBounds[name]);
        }

        ValidateBounds("Output", outputBounds);

        var configurationKey = JsonSerializer.Serialize(new
        {
          Inputs = inputNames
              .OrderBy(name => name, StringComparer.Ordinal)
              .Select(name => new { Name = name, Bounds = inputBounds[name] })
              .ToList(),
          OutputBounds = outputBounds,
          Objectives = _responseNames
        });

        if(_responseSpaces.Count != objectives.Count ||
           _currentRngSeed != rngSeed ||
           !string.Equals(_currentConfigurationKey, configurationKey, StringComparison.Ordinal))
        {
          _responseSpaces.Clear();
          _previousPoints.Clear();

          var configDict = inputNames.ToDictionary(name => name, name => inputBounds[name]);
          _randomizer = new Pcg64(rngSeed);
          _currentRngSeed = rngSeed;
          _currentConfigurationKey = configurationKey;

          for(int i = 0; i < objectives.Count; i++)
          {
            var numGaussians = _randomizer.Next(4, 12);
            var noiseScale = _randomizer.NextDouble(0.08, 0.25);
            var noiseFrequency = _randomizer.NextDouble(1.0, 10.0);
            var responseSeed = _randomizer.NextUInt64(1_000_000UL, 1_000_000_000_000UL);

            _responseSpaces.Add(new SyntheticProcessResponse(
                configDict,
                outputBounds,
                numGaussians,
                noiseScale,
                noiseFrequency,
                responseSeed));
          }
        }

        var results = new List<Objective>();
        var currentPoint = new Dictionary<string, double>(convertedInputDict);

        for(int i = 0; i < _responseSpaces.Count; i++)
        {
          var result = _responseSpaces[i].Evaluate(convertedInputDict);
          currentPoint[_responseNames[i]] = result;

          Console.WriteLine($"{_responseNames[i]}: {result}");
          results.Add(new Objective
          {
            ObjectiveName = _responseNames[i],
            ObjectiveValue = AresValueHelper.CreateFloat(result)
          });
        }

        _previousPoints.Add(currentPoint);

        var response = new AnalysisResponse
        {
          AnalysisOutcome = Outcome.Success,
          ErrorString = string.Empty
        };
        response.Objectives.AddRange(results);
        return response;
      }
      catch(Exception exception)
      {
        Console.WriteLine($"Error processing request: {exception.Message}");

        var response = new AnalysisResponse
        {
          AnalysisOutcome = Outcome.Failure,
          ErrorString = exception.Message
        };

        response.Objectives.AddRange(_responseNames.Select(name => new Objective
        {
          ObjectiveName = name,
          ObjectiveValue = AresValueHelper.CreateFloat(-1.0)
        }));

        return response;
      }
    }
  }

  private ulong ReadSeed(AnalysisRequest request)
  {
    if(_currentRngSeed.HasValue)
      return _currentRngSeed.Value;

    if(request.Settings != null &&
       request.Settings.Fields.TryGetValue("RNG Seed", out var seedValue))
    {
      if(!seedValue.TryGetNumericValue(out var numericSeed) ||
         !double.IsFinite(numericSeed) || numericSeed < 0.0 ||
         numericSeed > ulong.MaxValue)
      {
        throw new FormatException("RNG Seed must be a nonnegative finite number.");
      }

      return (ulong)numericSeed;
    }

    Console.WriteLine("No RNG seed value received, using a runtime seed for this analyzer");
    return (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
  }

  private static Dictionary<string, List<double>> ReadInputBounds(AnalysisRequest request, IReadOnlyCollection<string> inputNames)
  {
    var temperatureMaxValue = request.Settings.Fields.GetValueOrDefault(DemoSettings.TemperatureMax.Key);
    var temperatureMinValue = request.Settings.Fields.GetValueOrDefault(DemoSettings.TemperatureMin.Key);
    var flowRateMaxValue = request.Settings.Fields.GetValueOrDefault(DemoSettings.FlowRateMax.Key);
    var flowRateMinValue = request.Settings.Fields.GetValueOrDefault(DemoSettings.FlowRateMin.Key);

    var tempMax = 200.0;
    var tempMin = 0.0;
    var flowMax = 200.0;
    var flowMin = 0.0;

    temperatureMaxValue?.TryGetNumericValue(out tempMax);
    temperatureMinValue?.TryGetNumericValue(out tempMin);
    flowRateMaxValue?.TryGetNumericValue(out flowMax);
    flowRateMinValue?.TryGetNumericValue(out flowMin);

    var boundsDictionary = new Dictionary<string, List<double>>
    {
      { "Temperature", [tempMin, tempMax] },
      { "Flow Rate", [flowMin, flowMax] }
    };

    return boundsDictionary;
  }

  private static List<double> ReadOutputBounds(AnalysisRequest request)
  {
    if(request.Settings != null &&
       request.Settings.Fields.TryGetValue("Output Bounds", out var boundsValue))
    {
      var json = boundsValue.StringValue ?? string.Empty;
      return JsonSerializer.Deserialize<List<double>>(json)
          ?? throw new FormatException("Output Bounds must be a JSON array.");
    }

    return new List<double> { 0.0, 1.0 };
  }

  private static double ExtractNumericValue(AresValue value)
  {
    if(value.TryGetNumericValue(out var number) && double.IsFinite(number))
      return number;

    throw new FormatException("Analyzer inputs must be finite numeric values.");
  }

  private static void ValidateBounds(string name, IList<double> bounds)
  {
    if(bounds.Count < 2 ||
       !double.IsFinite(bounds[0]) ||
       !double.IsFinite(bounds[1]) ||
       bounds[0] >= bounds[1])
    {
      throw new FormatException(
          $"Bounds for '{name}' must contain two finite values in ascending order.");
    }
  }
}
