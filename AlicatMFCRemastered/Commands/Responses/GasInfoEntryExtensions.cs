using Ares.Alicat.Mfc.Messaging;

namespace AlicatMFCRemastered.Commands.Responses;

internal static class GasInfoEntryExtensions
{
  public static GasInfoEntry ToProto(this Streamed.GasInfoEntry entry)
  {
    var protoEntry = new GasInfoEntry();
    protoEntry.Name = entry.Gas;
    protoEntry.Index = entry.Index;
    return protoEntry;
  }
}
