using Ares.Messages.DeviceStates.SyringePump;
using CsvHelper.Configuration;

namespace AresService.DeviceStateExport.StreamProviders.SyringePump;

public class SyringePumpStateMap : ClassMap<SyringePumpState>
{
	public SyringePumpStateMap()
	{
		Map(m => m.Timestamp).Index(0).Name("Timestamp");
		Map(m => m.DeviceId).Index(1).Name("Device Id");
		Map(m => m.DispensedVolume).Index(2).Name("Dispensed Volume");
		Map(m => m.WithdrawnVolume).Index(3).Name("Withdrawn Volume");
		Map(m => m.VolumeUnit).Index(4).Name("Volume Unit");
		Map(m => m.Address).Index(5).Name("Address");
		Map(m => m.Status).Index(6).Name("Status");
	}
}
