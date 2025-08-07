namespace LindbergFurnace.Commands.Responses
{
    internal class ErrorResponse : CommandResponse 
    {
      public ErrorResponse(int address, FunctionCode functionCode, ErrorCode errorCode) : base(address, functionCode)
      {
        // NOTE: The function code in error responses seem to be FunctionCode + 0x80. Important for parsing into this message.
        ErrorCode = errorCode;
      }


      public ErrorCode ErrorCode { get; }
    }
}
