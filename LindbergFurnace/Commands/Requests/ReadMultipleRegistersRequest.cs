using LindbergFurnace.Commands.Responses;
using LindbergFurnace.Commands.Responses.Parsers;

namespace LindbergFurnace.Commands.Requests
{
    internal class ReadMultipleRegistersRequest : CommandExpectingResponse<ReadMultipleRegistersResponse>
    {
      public ReadMultipleRegistersRequest(int address, Register startRegister, int numRegisters) : base(address, FunctionCode.ReadMultiple, GenerateFunctionData(startRegister, numRegisters), new ReadMultipleReigstersResponseParser(address))
      {
        StartRegister = startRegister;
        NumRegisters = numRegisters;
      }

    private static byte[] GenerateFunctionData(Register startRegister, int numRegisters)
    {
      var messageData = new[] { (byte)((int)startRegister >> 8), (byte)(startRegister), (byte)(numRegisters << 8), (byte)(numRegisters) };
      return messageData;
    }

    public Register StartRegister { get; }
    public int NumRegisters { get; }
  }
}
