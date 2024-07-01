using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LindbergFurnace.Commands
{
  public static class TubeFurnaceCommandHelper
  {
    public static byte Lrc(IEnumerable<byte> data)
    {
      var sum = (byte)(data.Sum(b => b));
      var negative = ~sum;
      var complement = negative + 1;
      var lrc = (byte)complement;
      return lrc;
    }
  }
}
