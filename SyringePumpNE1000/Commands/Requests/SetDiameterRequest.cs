using SyringePumpNE1000.Commands.Responses;
using SyringePumpNE1000.Commands.Responses.Parsers;
using UnitsNet;

namespace SyringePumpNE1000.Commands.Requests;

internal class SetDiameterRequest : RequestExpectingResponse<Response>
{
  public SetDiameterRequest(int address, Length diameter) : base(new ConfirmationResponseParser(address))
  {
    Address = address;
    Diameter = diameter;
  }

  protected override string GenerateCommandString()
  {
    var diameterStr = FormatHelper.FormatToFloatString(Diameter.Millimeters);
    var commandData = $"{Address} {Ares.SyringePump.Ne1000.Messaging.Commands.Dia:G} {diameterStr}".ToUpperInvariant();
    return commandData;
  }

  public int Address { get; }
  public Length Diameter { get; }
}
