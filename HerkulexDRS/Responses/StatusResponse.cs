namespace HerkulexDRS.Responses;
public class StatusResponse : CommandResponse
{
  public StatusResponse(int servoId) : base(servoId)
  {
    ServoId = servoId;
  }

  public bool ServoHasError { get; set; }
}
