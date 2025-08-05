using System.Text.Json.Serialization;

namespace BoraasPlanner.BoraasTypes;
public class BoraasPlanResponse
{
  [JsonPropertyName(nameof(Message))]
  public string? Message { get; set; }

  [JsonPropertyName(nameof(ParamNames))]
  public string[]? ParamNames { get; set; }

  [JsonPropertyName(nameof(SolveFor))]
  public string[]? SolveFor { get; set; }

  [JsonPropertyName("Predicted Mean")]
  public object? PredictedMean { get; set; }

  [JsonPropertyName(nameof(Values))]
  public double[][]? Values { get; set; }

  [JsonPropertyName("execution time")]
  public double? ExecutionTime { get; set; }

  [JsonPropertyName("Lengthscale Estimate")]
  public double[][]? LengthScaleEstimate { get; set; }

  [JsonPropertyName("Noise Estimate")]
  public double[]? NoiseEstimate { get; set; }

  [JsonPropertyName("Predicted Variance")]
  public double[][]? PredictedVariance { get; set; }
}
