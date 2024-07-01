using ValveController.Commands.Responses;
using ValveController.DataModel;

namespace ValveController.Extensions;
public static class RelayStatusExtensions
{
  public static Data ToProto(this RelayStatusResponse response)
  {
    var data = new Data
    {
      RelayOneOn = response.RelayOneOn,
      RelayTwoOn = response.RelayTwoOn
    };

    return data;
  }
}
