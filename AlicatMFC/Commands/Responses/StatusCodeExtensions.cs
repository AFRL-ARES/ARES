using Ares.Alicat.Mfc.Messaging;

namespace AlicatMFC.Commands.Responses;

internal static class StatusCodeExtensions
{
  public static Status ToProto(this StatusCode internalStatus)
    => internalStatus switch
    {
      StatusCode.Na => Status.Na,
      StatusCode.Adc => Status.Adc,
      StatusCode.Exh => Status.Exh,
      StatusCode.Hld => Status.Hld,
      StatusCode.Lck => Status.Lck,
      StatusCode.Mov => Status.Mov,
      StatusCode.Opl => Status.Opl,
      StatusCode.Ovr => Status.Ovr,
      StatusCode.Pov => Status.Pov,
      StatusCode.Tov => Status.Tov,
      StatusCode.Tmf => Status.Tmf,
      StatusCode.Vov => Status.Vov,
      _ => throw new ArgumentOutOfRangeException(nameof(internalStatus), internalStatus, null)
    };

  public static StatusCode FromProto(this Status proto)
    => proto switch
    {
      Status.Na => StatusCode.Na,
      Status.Adc => StatusCode.Adc,
      Status.Lck => StatusCode.Lck,
      Status.Ovr => StatusCode.Ovr,
      Status.Pov => StatusCode.Pov,
      Status.Tov => StatusCode.Tov,
      Status.Vov => StatusCode.Vov,
      Status.Mov => StatusCode.Mov,
      Status.Hld => StatusCode.Hld,
      Status.Exh => StatusCode.Exh,
      Status.Tmf => StatusCode.Tmf,
      Status.Opl => StatusCode.Opl,
      _ => throw new ArgumentOutOfRangeException(nameof(proto), proto, null)
    };
}
