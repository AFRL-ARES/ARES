namespace AlicatMFCRemastered.Models;

public class MfcGasCompositionEntry
{
  public MfcGasCompositionEntry(int gasNumber, double percentage)
  {
    GasNumber = gasNumber;
    Percentage = percentage;
  }

  public int GasNumber { get; }
  public double Percentage { get; }
}
