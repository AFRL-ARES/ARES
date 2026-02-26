using AlicatMFCRemastered.Commands.Responses.Parsers;

namespace AlicatMFCRemastered.Commands.Responses.Streamed;

public class DataFrameFormatEntry : CommandResponse
{

  public DataFrameFormatEntry(
    char id,
    int entryNumber,
    DataFormatField field,
    string fieldType,
    string? minVal,
    string? maxVal,
    string? width,
    string notes,
    Enum? unit) : base(id)
  {
    EntryNumber = entryNumber;
    FieldType = fieldType;
    Field = field;
    MinVal = minVal;
    MaxVal = maxVal;
    Width = width;
    Unit = unit;
    Notes = notes;
  }

  public DataFrameFormatEntry(char id, DataFrameFormatEntryType entryType) : base(id)
  {
    EntryType = entryType;
    FieldType = string.Empty;
    Notes = string.Empty;
  }

  public DataFrameFormatEntryType EntryType { get; }
  public int EntryNumber { get; }
  public string FieldType { get; }
  public string? MinVal { get; }
  // max val has a setter as it may get overriden from a different source (like mfg info)
  public string? MaxVal { get; set; }
  public string? Width { get; }
  public string Notes { get; }
  public Enum? Unit { get; }
  public DataFormatField Field { get; }
}
