using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ares.Device;

namespace Ares.Core.Device
{
  public interface IDeviceCommandInterpreterRepo : ICollection<IDeviceCommandInterpreter<IAresDevice>>
  {
  }
}
