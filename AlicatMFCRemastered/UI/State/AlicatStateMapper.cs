using Ares.Datamodel;

namespace AlicatMFCRemastered.UI.State;

public static class AlicatStateMapper
{
  public static AlicatMfcState FromAresStruct(AresStruct state)
  {
    var model = new AlicatMfcState();

    // 1. Basic Metadata
    model.Id = state.Fields.GetValueOrDefault("Id")?.StringValue ?? "";
    model.Name = state.Fields.GetValueOrDefault("Name")?.StringValue ?? "";
    model.Firmware = state.Fields.GetValueOrDefault("Firmware")?.StringValue ?? "";
    model.HasValve = state.Fields.GetValueOrDefault("HasValve")?.BoolValue ?? false;
    model.ActiveGas = state.Fields.GetValueOrDefault("ActiveGas")?.StringValue ?? "Unknown";

    // 2. Unpack LiveData (Nested Struct)
    if(state.Fields.TryGetValue("LiveData", out var liveVal) && liveVal.StructValue != null)
    {
      var liveFields = liveVal.StructValue.Fields;
      model.LiveData = new MfcLiveData
      {
        AbsolutePressure = liveFields.GetValueOrDefault("AbsolutePressure")?.NumberValue ?? null,
        Temperature = liveFields.GetValueOrDefault("Temperature")?.NumberValue ?? 0,
        MassFlow = liveFields.GetValueOrDefault("MassFlow")?.NumberValue ?? 0,
        VolumetricFlow = liveFields.GetValueOrDefault("VolumetricFlow")?.NumberValue ?? null,
        Setpoint = liveFields.GetValueOrDefault("Setpoint")?.NumberValue ?? 0,
        ValveDrive = liveFields.GetValueOrDefault("ValveDrive")?.NumberValue ?? null,
        StatusCodes = liveFields.GetValueOrDefault("StatusCodes")?.ListValue?.Values?.Select(v => v.StringValue).ToList() ?? new List<string>()
      };
    }

    // 3. Unpack Gases (List of Structs)
    if(state.Fields.TryGetValue("Gases", out var gasListVal) && gasListVal.ListValue != null)
    {
      model.Gases = gasListVal.ListValue.Values
          .Where(v => v.StructValue != null)
          .Select(v => new MfcGasInfo
          {
            Gas = v.StructValue!.Fields.GetValueOrDefault("Gas")?.StringValue ?? "",
            Index = (int)(v.StructValue.Fields.GetValueOrDefault("Index")?.NumberValue ?? 0),
            Id = v.StructValue.Fields.GetValueOrDefault("Id")?.StringValue ?? ""
          }).ToList();
    }

    // 4. Unpack Manufacturer Info (List of Structs)
    if(state.Fields.TryGetValue("ManufacturerInfo", out var mfListVal) && mfListVal.ListValue != null)
    {
      model.ManufacturerInfo = mfListVal.ListValue.Values
          .Where(v => v.StructValue != null)
          .Select(v => new MfcManufacturerEntry
          {
            EntryNumber = (int)(v.StructValue!.Fields.GetValueOrDefault("EntryNumber")?.NumberValue ?? 0),
            Category = v.StructValue.Fields.GetValueOrDefault("Manufacturer")?.StringValue ?? "",
            Data = v.StructValue.Fields.GetValueOrDefault("Data")?.StringValue ?? ""
          }).ToList();
    }

    return model;
  }
}