namespace AlicatMFCRemastered.Commands.Responses;

/// <summary>
/// Source: DOC-SERIALPRIMER rev1 -- August 25, 2020
/// </summary>
public static class StatusCodeDescriptionProvider
{
  public static string GetDescription(StatusCode code)
  {
    return code switch
    {
      StatusCode.Adc => "Internal communication error (not common – requires repair at factory)",
      StatusCode.Exh => "Manual exhaust valve override (max drive on downstream valve)",
      StatusCode.Hld => "Valve drive hold is active (bypass active PID control)",
      StatusCode.Lck => "Membrane button lockout is active (see command codes below)",
      StatusCode.Mov => "Mass flow rate overage (outside measurable range including uncalibrated range)",
      StatusCode.Opl => "Over pressure limit has been activated",
      StatusCode.Ovr => "Totalizer has rolled over at least once or frozen at max value",
      StatusCode.Pov => "Pressure reading overage (outside measurable range including uncalibrated\r\nrange)",
      StatusCode.Tov => "Temperature reading overage (outside measurable range)",
      StatusCode.Tmf => "Totalizer missed some flow data (due to MOV or VOV error)",
      StatusCode.Vov => "Volumetric flow rate overage (outside measurable range including uncalibrated\r\nrange",
      StatusCode.Na => "Unknown",
      _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
    };
  }
}
