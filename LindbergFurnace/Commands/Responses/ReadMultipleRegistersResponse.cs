using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LindbergFurnace.Commands.Responses
{
    internal class ReadMultipleRegistersResponse : CommandResponse
    {

      public ReadMultipleRegistersResponse(int address, FunctionCode functionCode, int byteCount, byte[][] registerContents) : base(address, functionCode)
      {
        ByteCount = byteCount;
        RegisterContents = registerContents;
      }

      public int ByteCount { get; }
      public byte[][] RegisterContents { get; }
    }
}
