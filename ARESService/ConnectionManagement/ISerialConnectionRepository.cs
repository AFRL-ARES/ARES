using Ares.Device;
using System.Collections.Generic;

namespace AresService.ConnectionManagement;

public interface ISerialConnectionRepository : ICollection<IAresDeviceConnection>
{
}
