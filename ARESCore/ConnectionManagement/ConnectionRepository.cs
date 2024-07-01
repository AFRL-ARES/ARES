using System.Collections.Generic;
using Ares.Device.Serial;

namespace ARESCore.ConnectionManagement;

public class ConnectionRepository : List<IAresSerialConnection>, IConnectionRepository
{
}
