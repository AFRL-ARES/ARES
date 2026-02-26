
using System.Text.RegularExpressions;
using AlicatMFCRemastered.Enums;
using Ares.Toolkit.Serial.Commands;

namespace AlicatMFCRemastered.Commands.Responses.Parsers;

internal class ManufactureInfoResponseEntryParser : AsciiResponseParser<ManufacturerInfoEntry>
{
  private readonly char _assumedId;
  private readonly int? _lineNum;
  internal ManufactureInfoResponseEntryParser(char assumedId, int? lineNum)
  {
    _assumedId = assumedId;
    _lineNum = lineNum;
  }
  protected override bool TryParseResponse(string line, out ManufacturerInfoEntry? response)
  {
    var regexMatch = Regex.Match(line, @"(?<UnitId>[A-Z])\s+M(?<LineNumber>\d+)\s+(?<LineValue>.*)");
    var lineNumberFound = regexMatch.Groups.TryGetValue("LineNumber", out var lineNumberGroup);
    if(!lineNumberFound || lineNumberGroup is null)
    {
      response = null;
      return false;
    }

    _ = regexMatch.Groups.TryGetValue("LineValue", out var lineValueGroup);
    _ = regexMatch.Groups.TryGetValue("UnitId", out var unitIdGroup);

    if(lineValueGroup is null || unitIdGroup is null)
    {
      response = null;
      return false;
    }

    var lineNumber = int.Parse(lineNumberGroup.Value);

    if(_lineNum.HasValue && lineNumber != _lineNum.Value)
    {
      response = null;
      return false;
    }
    var unitId = unitIdGroup.Value[0];
    if(unitId != _assumedId)
    {
      response = null;
      return false;
    }
    var lineValue = lineValueGroup.Value;
    lineValue = AlphaNumericize(lineValue);
    if(lineNumber == 0)
    {
      response = new ManufacturerInfoEntry(unitId, lineNumber, ManufacturerInfoEntryTypeEnum.Title, lineValue);
      return true;
    }

    if(lineValue.StartsWith("ph", StringComparison.InvariantCultureIgnoreCase))
    {
      var phoneNumber = lineValue.Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries).Last();
      response = new ManufacturerInfoEntry(unitId, lineNumber, ManufacturerInfoEntryTypeEnum.PhoneNumber, phoneNumber);
      return true;
    }

    if(lineValue.StartsWith("fax", StringComparison.InvariantCultureIgnoreCase))
    {
      var faxNumber = lineValue.Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries).Last();
      response = new ManufacturerInfoEntry(unitId, lineNumber, ManufacturerInfoEntryTypeEnum.Fax, faxNumber);
      return true;
    }

    if(lineValue.Contains("model number", StringComparison.InvariantCultureIgnoreCase) || lineValue.Contains("mdl", StringComparison.InvariantCultureIgnoreCase))
    {
      var modelNumber = lineValue.Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries).Last();
      response = new ManufacturerInfoEntry(unitId, lineNumber, ManufacturerInfoEntryTypeEnum.ModelNumber, modelNumber);
      return true;
    }

    if(lineValue.StartsWith("serial number", StringComparison.InvariantCultureIgnoreCase))
    {
      var serialNumber = lineValue.Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries).Last();
      response = new ManufacturerInfoEntry(unitId, lineNumber, ManufacturerInfoEntryTypeEnum.SerialNumber, serialNumber);
      return true;
    }

    if(lineValue.StartsWith("date manufactured", StringComparison.InvariantCultureIgnoreCase))
    {
      _ = DateTime.TryParse(lineValue.Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries).Last(), out var manufactureDate);
      response = new ManufacturerInfoEntry(unitId, lineNumber, ManufacturerInfoEntryTypeEnum.ManufactureDate, manufactureDate.ToString());
      return true;
    }

    if(lineValue.StartsWith("date calibrated", StringComparison.InvariantCultureIgnoreCase))
    {
      _ = DateTime.TryParse(lineValue.Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries).Last(), out var calibrationDate);
      response = new ManufacturerInfoEntry(unitId, lineNumber, ManufacturerInfoEntryTypeEnum.CalibrationDate, calibrationDate.ToString());
      return true;
    }

    if(lineValue.StartsWith("software revision", StringComparison.InvariantCultureIgnoreCase))
    {
      var softwareRevision = lineValue.Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries).Last();
      response = new ManufacturerInfoEntry(unitId, lineNumber, ManufacturerInfoEntryTypeEnum.CalibrationDate, softwareRevision);
      return true;
    }

    if(lineValue.StartsWith("calibrated by", StringComparison.InvariantCultureIgnoreCase))
    {
      var calibratedBy = lineValue.Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries).Last();
      response = new ManufacturerInfoEntry(unitId, lineNumber, ManufacturerInfoEntryTypeEnum.CalibratedBy, calibratedBy);
      return true;
    }

    response = new ManufacturerInfoEntry(unitId, lineNumber, ManufacturerInfoEntryTypeEnum.Invalid, lineValue.Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries).Last());
    return true;
  }

  private string AlphaNumericize(string line)
    => new string(line.Where(chr => chr == '_' || (chr >= 'a' && chr <= 'z') || (chr >= 'A' && chr <= 'Z') || (chr >= '0' && chr <= '9') || chr == '-' || chr == '+' || chr == '\t' || chr == '\r' || chr == ' ').ToArray());
}
