using AlicatMFC.Commands.Responses;
using Ares.Alicat.Mfc.Messaging;

namespace AlicatMFC;

public static class MfcStateExtensions
{
  public static StateResponse ToProto(this MfcState state)
  {
    var response = new StateResponse { AssumedId = state.Id.ToString() };
    response.AvailableGasInfos.AddRange(state.Gases?.Select(gas => gas.ToProto()) ?? Array.Empty<GasInfoEntry>());
    response.Data = state.LiveData?.ToProto();
    response.DataFormatEntries.AddRange(state.DataFrameFormatEntries?.Select(entry => entry.ToProto()) ?? Array.Empty<DataFrameFormatEntry>());
    response.HasValve = state.HasValve;
    return response;
  }
}
