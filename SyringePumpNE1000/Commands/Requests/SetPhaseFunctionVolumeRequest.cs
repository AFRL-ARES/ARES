using SyringePumpNE1000.Commands.Responses;
using SyringePumpNE1000.Commands.Responses.Parsers;
using UnitsNet;

namespace SyringePumpNE1000.Commands.Requests;

internal class SetPhaseFunctionVolumeRequest : RequestExpectingResponse<Response>
{

  public SetPhaseFunctionVolumeRequest(int address, Volume volume) : base(new ConfirmationResponseParser(address))
  {
    Address = address;
    Volume = volume;
  }

  protected override string GenerateCommandString()
  {
    var commandFloatDataStr = FormatHelper.FormatToFloatString(Volume.Milliliters);
    var commandData = $"{Address} {Ares.SyringePump.Ne1000.Messaging.Commands.Vol} {commandFloatDataStr}".ToUpperInvariant();
    return commandData;
  }
  public int Address { get; }
  public Volume Volume { get; }
}
