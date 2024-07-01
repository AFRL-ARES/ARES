using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ares.Device.Serial
{
  internal record SerialBlock(byte[] Data, DateTime Timestamp);
}
