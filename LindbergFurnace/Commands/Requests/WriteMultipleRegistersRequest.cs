using LindbergFurnace.Commands.Responses;
using LindbergFurnace.Commands.Responses.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LindbergFurnace.Commands.Requests
{
  public class WriteMultipleRegistersRequest : CommandExpectingResponse<WriteMultipleRegistersResponse>
  {
    public WriteMultipleRegistersRequest(int address, params RegisterReadWrite[] registerWrites) : base(address, FunctionCode.WriteMultiple, GenerateFunctionData(registerWrites), new WriteMultipleRegistersResponseParser(address))
    {
      RegisterWrites = registerWrites;
    }

    private static byte[] GenerateFunctionData(RegisterReadWrite[] registerWrites)
    {
      var orderedWrites = registerWrites.OrderBy(reg => (int)reg.Register).ToArray();
      var startRegister = orderedWrites[0].Register;
      var registerStartNumberUpper = (byte)((int)startRegister >> 8);
      var registartStartNumberLower = (byte)(startRegister);
      var numRegisters = orderedWrites.Length;
      var numRegistersUpper = (byte)(numRegisters << 8);
      var numRegistersLower = (byte)(numRegisters);
      var byteCount = (byte) (numRegisters * 2); // TODO: Make sure it's 2, not 4 nor 8. This is another part of where the documentation is inconsistent
      var messageData = new List<byte> { registerStartNumberUpper, registartStartNumberLower, numRegistersUpper, numRegistersLower, byteCount };
      var data = orderedWrites.SelectMany(write => new[] { write.UpperDigit!.Value, write.LowerDigit!.Value }).ToArray();
      messageData.AddRange(data);
      return messageData.ToArray();
    }

    public RegisterReadWrite[] RegisterWrites { get; }
  }
}
