using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LindbergFurnace.Commands
{
    public enum FunctionCode
    {
      Undefined,
      ReadMultiple = 3,
      WriteSingle = 6,
      LoopBackTest = 8,
      WriteMultiple = 16
    }
}
