using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LindbergFurnace.Commands.Responses
{
  public class WriteMultipleRegistersResponse : CommandResponse
  {

    public WriteMultipleRegistersResponse(int address, FunctionCode functionCode, params Register[] registers) : base(address, functionCode)
    {
      Registers = registers;
    }

    public Register[] Registers { get; set; }
  }
}
