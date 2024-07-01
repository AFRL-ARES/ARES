using System.ComponentModel.DataAnnotations;

namespace UI.Backend.ViewModels.Settings.Device;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class CharacterRangeAttribute : ValidationAttribute
{
  private readonly char _max;
  private readonly char _min;

  public CharacterRangeAttribute(char min, char max)
  {
    _min = min;
    _max = max;
  }

  protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
  {
    switch (value)
    {
      case string str when string.IsNullOrEmpty(str):
        return ValidationResult.Success;
      case string str when str.Length != 1:
        return new ValidationResult("Input must be a single character.");
      case string str:
        {
          var character = str.First();
          return CheckChar(character);
        }
      case char chr:
        return CheckChar(chr);
    }

    return new ValidationResult("Bork");
  }

  private ValidationResult? CheckChar(char character)
  {
    if (character >= _min && character <= _max)
      return ValidationResult.Success;

    return new ValidationResult($"The character must be between {_min} and {_max}");
  }
}
