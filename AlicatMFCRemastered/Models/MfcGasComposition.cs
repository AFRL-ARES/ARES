namespace AlicatMFCRemastered.Models;

public class MfcGasComposition
{
  public MfcGasComposition(string name, int number, MfcGasCompositionEntry[] entries)
  {
    Name = name;
    Number = number;
    Entries = entries;
  }

  public string Name { get; }
  public int Number { get; }
  public MfcGasCompositionEntry[] Entries { get; }
}
