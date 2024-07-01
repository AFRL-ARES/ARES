using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LindbergFurnace.Commands
{
  public class RegisterReadWrite
  {
    public Register Register { get; set; }
    public byte? UpperDigit { get; set; }
    public byte? LowerDigit { get; set; }
  }
}
