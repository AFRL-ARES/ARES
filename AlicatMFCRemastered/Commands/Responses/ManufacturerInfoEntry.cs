using AlicatMFCRemastered.Enums;

namespace AlicatMFCRemastered.Commands.Responses;

public class ManufacturerInfoEntry : CommandResponse
{

  public ManufacturerInfoEntry(char id, int entryNumber, ManufacturerInfoEntryTypeEnum manufacturerInfoEntryType, string data) : base(id)
  {
    EntryNumber = entryNumber;
    ManufacturerInfoEntryType = manufacturerInfoEntryType;
    Data = data;
  }

  public ManufacturerInfoEntry(char id) : base(id)
  {
    IsEndMarker = true;
  }

  public ManufacturerInfoEntryTypeEnum ManufacturerInfoEntryType { get; }
  public string Data { get; } = string.Empty;
  public int EntryNumber { get; }
  public bool IsEndMarker { get; }
}
