using Ares.Alicat.Mfc.Messaging;

namespace AlicatMFCRemastered.Commands.Responses;

public class ManufacturerInfoEntry : CommandResponse
{

  public ManufacturerInfoEntry(char id, int entryNumber, ManufacturerInfoEntryType manufacturerInfoEntryType, string data) : base(id)
  {
    EntryNumber = entryNumber;
    ManufacturerInfoEntryType = manufacturerInfoEntryType;
    Data = data;
  }

  public ManufacturerInfoEntry(char id) : base(id)
  {
    IsEndMarker = true;
  }

  public ManufacturerInfoEntryType ManufacturerInfoEntryType { get; }
  public string Data { get; } = string.Empty;
  public int EntryNumber { get; }
  public bool IsEndMarker { get; }
}
