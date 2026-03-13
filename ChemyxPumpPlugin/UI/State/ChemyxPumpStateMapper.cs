using Ares.Datamodel;
using Ares.Datamodel.Extensions;

namespace ChemyxPumpPlugin.UI.State;

public static class ChemyxPumpStateMapper
{
  public static ChemyxPumpState FromAresStruct(AresStruct state)
  {
    var model = new ChemyxPumpState();

    model.Name = state.Fields.GetValueOrDefault("Name")?.StringValue ?? "";
    model.DualPump = state.Fields.GetValueOrDefault("DualPump")?.BoolValue ?? false;

    if(state.Fields.TryGetValue("Pumps", out var pumpsVal) && pumpsVal.ListValue != null)
    {
      model.Pumps = pumpsVal.ListValue.Values
        .Where(v => v.StructValue != null)
        .Select(v => new PumpData
        {
          Index = (int)(v.StructValue!.Fields.GetValueOrDefault("Index")?.NumberValue ?? 0),
          Status = v.StructValue.Fields.GetValueOrDefault("Status")?.StringValue ?? "Unknown",
          Volume = v.StructValue.Fields.GetValueOrDefault("Volume")?.NumberValue ?? 0,
          Time = v.StructValue.Fields.GetValueOrDefault("Time")?.StringValue ?? "00:00:00",
          Diameter = v.StructValue.Fields.GetValueOrDefault("Diameter")?.NumberValue ?? 0,
          TargetVolume = v.StructValue.Fields.GetValueOrDefault("TargetVolume")?.NumberValue ?? 0,
          Rate = v.StructValue.Fields.GetValueOrDefault("Rate")?.NumberValue ?? 0,
          Delay = v.StructValue.Fields.GetValueOrDefault("Delay")?.NumberValue ?? 0,
          Units = v.StructValue.Fields.GetValueOrDefault("Units")?.StringValue ?? ""
        }).ToList();
    }

    return model;
  }
}
