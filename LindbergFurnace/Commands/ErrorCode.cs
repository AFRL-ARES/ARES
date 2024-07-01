using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LindbergFurnace.Commands
{
    internal enum ErrorCode
    {
      Undefined = -1,
      FunctionCode = 0x01,
      RegisterAddress = 0x02,
      RegisterCount = 0x03,
    }
}
