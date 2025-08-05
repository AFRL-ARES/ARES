using System.IO;

namespace AresService.DeviceStateExport.StreamProviders;

public record DeviceStateStream(string Name, Stream Stream);
