using Ares.Alicat.Mfc.Messaging;

namespace AlicatMFC.Commands.Responses;

internal static class DataFrameFormatExtensions
{
  public static DataFrameFormatEntry ToProto(this Streamed.DataFrameFormatEntry internalEntry)
  {
    var entry = new DataFrameFormatEntry
    {
      Name = internalEntry.Field.ToString(),
      Type = internalEntry.FieldType,
      Id = $"{internalEntry.Id}",
      MinimumValue = internalEntry.MinVal ?? string.Empty,
      MaximumValue = internalEntry.MaxVal ?? string.Empty,
      LineNumber = internalEntry.EntryNumber,
      Units = internalEntry.Unit?.ToString() ?? string.Empty
    };

    return entry;
  }
}
