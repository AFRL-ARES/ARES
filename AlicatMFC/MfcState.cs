using AlicatMFC.Commands.Responses;
using AlicatMFC.Commands.Responses.Streamed;

namespace AlicatMFC;

public record MfcState(char Id, string Name)
{
  public IEnumerable<ManufacturerInfoEntry>? ManufacturerInfo { get; init; }
  public IEnumerable<GasInfoEntry>? Gases { get; init; }
  public IEnumerable<DataFrameFormatEntry>? DataFrameFormatEntries { get; init; }
  public LiveDataResponse? LiveData { get; init; }
  public bool HasValve { get; set; }
}
