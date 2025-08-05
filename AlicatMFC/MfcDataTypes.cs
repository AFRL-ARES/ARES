using Ares.Messaging;

namespace AlicatMFC;
public static class MfcDataTypes
{
  public static readonly KeyValuePair<string, AresDataType> Setpoint = new KeyValuePair<string, AresDataType>(
    "Setpoint",
    AresDataType.Number);
}
