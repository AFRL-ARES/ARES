using Ares.Datamodel;

namespace AlicatMFCRemastered.Commands.Types;

public static class MfcDataTypes
{
  public static readonly KeyValuePair<string, AresDataType> Setpoint = new KeyValuePair<string, AresDataType>(
    "Setpoint",
    AresDataType.Number);
}

