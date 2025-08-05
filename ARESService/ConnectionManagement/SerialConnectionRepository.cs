using Ares.Device;
using System.Collections.Generic;

namespace AresService.ConnectionManagement;

public class SerialConnectionRepository : List<IAresDeviceConnection>, ISerialConnectionRepository
{
}
