using System.Collections.Generic;
using Ares.Device.Serial;

namespace ARESCore.ConnectionManagement;

public interface IConnectionRepository : ICollection<IAresSerialConnection>
{
}
