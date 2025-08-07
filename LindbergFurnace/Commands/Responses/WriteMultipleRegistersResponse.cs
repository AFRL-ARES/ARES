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
