namespace Ares.Device.Serial.Commands
{
    public abstract class SerialCommandWithStreamedResponse<TCommandResponse> : SerialCommandWithResponse<TCommandResponse> where TCommandResponse : SerialResponse
    {
        protected SerialCommandWithStreamedResponse(SerialResponseParser<TCommandResponse> parser) : base(parser)
        {
        }
    }
}
