using System.Text.Json.Serialization;

namespace BoraasPlanner.BoraasTypes;
public class BoraasPlanRequest
{
  [JsonPropertyName(nameof(ParamNames))]
  public List<string>? ParamNames { get; set; }

  [JsonPropertyName(nameof(History))]
  public List<List<double>>? History { get; set; }

  [JsonPropertyName(nameof(SolveFor))]
  public List<string>? SolveFor { get; set; }

  [JsonPropertyName(nameof(MinVals))]
  public List<double>? MinVals { get; set; }

  [JsonPropertyName(nameof(MaxVals))]
  public List<double>? MaxVals { get; set; }

  [JsonPropertyName(nameof(Key))]
  public string? Key { get; set; }

  [JsonPropertyName(nameof(NumExpsRequested))]
  public int? NumExpsRequested { get; set; }

  //[JsonPropertyName(nameof(SobolSeeds))]
  //public int? SobolSeeds { get; set; }

  //[JsonPropertyName("Burn-in")]
  //public int? BurnIn { get; set; }

  [JsonPropertyName("test")]
  public bool? Test { get; set; }
}
